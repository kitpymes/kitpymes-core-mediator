// -----------------------------------------------------------------------
// <copyright file="MediatorServiceCollectionExtensions.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Mediator.Internal;
using Kitpymes.Core.Mediator.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Clase de extensión <c>MediatorServiceCollectionExtensions</c>.
/// Contiene las extensiones de los servicios del mediador.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    private const string ReflectionMessage = "El mediador busca manejadores por reflexión y crea tipos genéricos en ejecución: no es compatible con trimming ni NativeAOT.";

    /// <summary>
    /// Carga el mediador.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="options">Configuración del mediador.</param>
    /// <returns>IServiceCollection.</returns>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static IServiceCollection LoadMediator(this IServiceCollection services, Action<MediatorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        var configured = new MediatorOptions();
        options(configured);

        return services.LoadMediator(configured.MediatorSettings);
    }

    /// <summary>
    /// Carga el mediador.
    /// </summary>
    /// <remarks>
    /// Registra <see cref="ISender"/> e <see cref="IPublisher"/> con ámbito (scoped) y, en cada ensamblado, los manejadores
    /// de comandos, consultas y notificaciones (transient). Llamarlo dos veces no duplica nada. La comprobación de
    /// validación y la de arranque quedan activas si cualquiera de las llamadas las pide. Los comportamientos los
    /// registra quien lo usa: <c>services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(MiComportamiento&lt;,&gt;))</c>.
    /// </remarks>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="settings">Configuración del mediador.</param>
    /// <returns>IServiceCollection.</returns>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static IServiceCollection LoadMediator(this IServiceCollection services, MediatorSettings? settings)
    {
        ArgumentNullException.ThrowIfNull(services);

        settings ??= new MediatorSettings();

        var catalog = services
            .Select(x => x.ImplementationInstance)
            .OfType<MediatorCatalog>()
            .FirstOrDefault();

        if (catalog is null)
        {
            catalog = new MediatorCatalog();
            services.AddSingleton(catalog);
            services.AddSingleton<IMediatorCatalog>(catalog);
        }

        catalog.Require(settings.Validation);

        foreach (var assembly in settings.Assemblies.Where(x => x is not null).OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            if (catalog.AddAssembly(assembly))
            {
                Scan(services, catalog, assembly);
            }
        }

        services.TryAddScoped<Internal.Mediator>();
        services.TryAddScoped<ISender>(x => x.GetRequiredService<Internal.Mediator>());
        services.TryAddScoped<IPublisher>(x => x.GetRequiredService<Internal.Mediator>());

        catalog.RequireStartupVerification(settings.StartupVerification);

        // Uno de los dos: con la comprobación, la ejecuta; sin ella, avisa en el log al arrancar.
        services.TryAddEnumerable(settings.StartupVerification
            ? ServiceDescriptor.Singleton<IHostedService, MediatorStartupVerifier>()
            : ServiceDescriptor.Singleton<IHostedService, MediatorStartupWarning>());

        return services;
    }

    private static void Scan(IServiceCollection services, MediatorCatalog catalog, Assembly assembly)
    {
        foreach (var type in LoadableTypes(assembly)
            .Where(x => !x.IsAbstract && !x.IsInterface && !x.IsGenericTypeDefinition)
            .OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            catalog.AddMessage(type);

            foreach (var contract in type.GetInterfaces().Where(x => x.IsGenericType))
            {
                var definition = contract.GetGenericTypeDefinition();

                if (definition == typeof(ICommandHandler<,>) || definition == typeof(IQueryHandler<,>) || definition == typeof(INotificationHandler<>))
                {
                    services.TryAddEnumerable(ServiceDescriptor.Transient(contract, type));
                    catalog.AddHandler(contract.GetGenericArguments()[0], contract);
                }
            }
        }
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(x => x is not null).Select(x => x!);
        }
    }
}
