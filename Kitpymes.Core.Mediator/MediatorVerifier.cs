// -----------------------------------------------------------------------
// <copyright file="MediatorVerifier.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Mediator.Internal;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Comprueba que el mediador está bien configurado. Se ejecuta al arrancar el host y se usa en los tests de arquitectura,
/// para que un manejador que falta pare la pipeline y no llegue a producción.
/// </summary>
public static class MediatorVerifier
{
    /// <summary>
    /// Comprueba la configuración del mediador.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Todo comando y consulta tiene exactamente un manejador, y él y sus comportamientos se pueden crear.</item>
    /// <item>Ninguna petición es a la vez comando y consulta, ni tiene dos resultados.</item>
    /// <item>Si la validación está activa, hay una <see cref="IValidationStartupCheck"/> registrada.</item>
    /// <item>Todas las <see cref="IStartupCheck"/> registradas pasan.</item>
    /// </list>
    /// </remarks>
    /// <param name="provider">Proveedor de servicios ya construido.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Éxito, o un fallo con una línea por problema.</returns>
    public static async Task<Result> VerifyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var catalog = provider.GetService<MediatorCatalog>();

        if (catalog is null)
        {
            return Failure(new[] { "No se ha llamado a LoadMediator." });
        }

        var problems = new List<string>();

        if (catalog.Assemblies.Count == 0)
        {
            problems.Add("LoadMediator no tiene ningún ensamblado: use WithAssemblies(...).");
        }

        problems.AddRange(catalog.InvalidRequests());

        var scope = provider.CreateAsyncScope();

        try
        {
            var services = scope.ServiceProvider;

            foreach (var command in catalog.Commands.OrderBy(x => x.FullName, StringComparer.Ordinal))
            {
                CheckRequest(services, command, typeof(ICommand<>), typeof(ICommandHandler<,>), typeof(ICommandBehavior<,>), problems);
            }

            foreach (var query in catalog.Queries.OrderBy(x => x.FullName, StringComparer.Ordinal))
            {
                CheckRequest(services, query, typeof(IQuery<>), typeof(IQueryHandler<,>), typeof(IQueryBehavior<,>), problems);
            }

            foreach (var service in catalog.HandlerServices.Where(x => x.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            {
                TryResolve(services, service, $"los manejadores de {MediatorCatalog.Name(service.GetGenericArguments()[0])}", problems);
            }

            await CheckStartupChecksAsync(services, catalog, problems, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                await scope.DisposeAsync().ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Un fallo al cerrar el ámbito es un problema más del informe.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                problems.Add($"Al cerrar el ámbito de la comprobación: {exception.GetType().Name}: {exception.Message}");
            }
        }

        return problems.Count == 0 ? Result.Success() : Failure(problems);
    }

    private static void CheckRequest(IServiceProvider services, Type request, Type contract, Type handlerDefinition, Type behaviorDefinition, List<string> problems)
    {
        foreach (var result in request.GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == contract)
            .Select(x => x.GetGenericArguments()[0]))
        {
            var name = $"{MediatorCatalog.Name(request)} ({MediatorCatalog.Name(result)})";
            var handlers = TryResolve(services, handlerDefinition.MakeGenericType(request, result), $"el manejador de {name}", problems);

            if (handlers is not null && handlers.Count == 0)
            {
                problems.Add($"{name} no tiene manejador.");
            }
            else if (handlers is not null && handlers.Count > 1)
            {
                problems.Add($"{name} tiene {handlers.Count} manejadores ({string.Join(", ", handlers.Select(x => x!.GetType().Name))}); tiene que tener exactamente uno.");
            }

            TryResolve(services, typeof(IPipelineBehavior<,>).MakeGenericType(request, result), $"los comportamientos de {name}", problems);
            TryResolve(services, behaviorDefinition.MakeGenericType(request, result), $"los comportamientos de {name}", problems);
        }
    }

    private static List<object?>? TryResolve(IServiceProvider services, Type service, string what, List<string> problems)
    {
        try
        {
            return services.GetServices(service).ToList();
        }
#pragma warning disable CA1031 // Una dependencia que falta es un problema del informe, no una excepción.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            problems.Add($"No se pueden crear {what}: {exception.Message}");
            return null;
        }
    }

    private static async Task CheckStartupChecksAsync(IServiceProvider services, MediatorCatalog catalog, List<string> problems, CancellationToken cancellationToken)
    {
        // Una comprobación por tipo, aunque esté registrada como IStartupCheck y como IValidationStartupCheck.
        var checks = new List<IStartupCheck>();

        foreach (var check in (TryResolve(services, typeof(IStartupCheck), "las comprobaciones de arranque", problems) ?? new List<object?>())
            .Concat(TryResolve(services, typeof(IValidationStartupCheck), "la comprobación de validación", problems) ?? new List<object?>())
            .OfType<IStartupCheck>())
        {
            if (!checks.Exists(x => x.GetType() == check.GetType()))
            {
                checks.Add(check);
            }
        }

        if (catalog.RequireValidation && !checks.OfType<IValidationStartupCheck>().Any())
        {
            problems.Add("Falta la comprobación de validación: registre la validación (Kitpymes.Core.Validations) o desactívela con WithValidation(false).");
        }

        foreach (var check in checks)
        {
            try
            {
                var task = check.CheckAsync(catalog, services, cancellationToken);
                var result = task is null ? null : await task.ConfigureAwait(false);

                if (result is null)
                {
                    problems.Add($"{check.GetType().FullName} devolvió un resultado nulo.");
                }
                else if (result.IsFailure)
                {
                    problems.Add($"{check.GetType().Name}: {result.Error.Message}");
                }
            }
#pragma warning disable CA1031 // Una comprobación que falla es un problema del informe.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                problems.Add($"{check.GetType().FullName} lanzó {exception.GetType().Name}: {exception.Message}");
            }
        }
    }

    private static Result Failure(IEnumerable<string> problems)
        => Result.Failure(Error.Unexpected(
            MediatorErrors.Configuration,
            "El mediador no está bien configurado:" + string.Concat(problems.Select(x => Environment.NewLine + "- " + x))));
}
