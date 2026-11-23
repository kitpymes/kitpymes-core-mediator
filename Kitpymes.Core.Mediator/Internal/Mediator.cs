// -----------------------------------------------------------------------
// <copyright file="Mediator.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Implementación de <see cref="ISender"/> y <see cref="IPublisher"/>. Se registra con ámbito (scoped): resuelve los
/// manejadores en el ámbito de la petición, nunca en el raíz.
/// </summary>
internal sealed class Mediator : ISender, IPublisher
{
    private static readonly ConcurrentDictionary<Type, NotificationWrapper> NotificationWrappers = new ();

    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="Mediator"/>.
    /// </summary>
    /// <param name="services">Proveedor del ámbito actual.</param>
    public Mediator(IServiceProvider services)
    {
        _services = services;
        _logger = (ILogger?)services.GetService<ILogger<Mediator>>() ?? NullLogger.Instance;
    }

    /// <inheritdoc/>
    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(command);

        var type = command.GetType();

        // Sin lambda: la creación del envoltorio, la resolución y la llamada van dentro del try,
        // y la captura común se aplica después (ver Pipeline.CompleteAsync).
        Task<TResult>? task = null;
        Exception? error = null;

        try
        {
            task = Wrappers<TResult>.Command(type).HandleAsync(command, _services, cancellationToken);
        }
#pragma warning disable CA1031 // Se relanza dentro de Pipeline.CompleteAsync, con la captura común.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            error = exception;
        }

        return Pipeline.CompleteAsync(task, error, type, _logger, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(query);

        var type = query.GetType();

        // Sin lambda: la creación del envoltorio, la resolución y la llamada van dentro del try,
        // y la captura común se aplica después (ver Pipeline.CompleteAsync).
        Task<TResult>? task = null;
        Exception? error = null;

        try
        {
            task = Wrappers<TResult>.Query(type).HandleAsync(query, _services, cancellationToken);
        }
#pragma warning disable CA1031 // Se relanza dentro de Pipeline.CompleteAsync, con la captura común.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            error = exception;
        }

        return Pipeline.CompleteAsync(task, error, type, _logger, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result> PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var type = notification.GetType();

        Task<Result>? task = null;
        Exception? error = null;

        try
        {
            task = NotificationWrappers
                .GetOrAdd(type, static t => Create<NotificationWrapper>(typeof(NotificationWrapper<>).MakeGenericType(t)))
                .PublishAsync(notification, _services, _logger, cancellationToken);
        }
#pragma warning disable CA1031 // Se relanza dentro de Pipeline.CompleteAsync, con la captura común.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            error = exception;
        }

        return Pipeline.CompleteAsync(task, error, type, _logger, cancellationToken);
    }

    private static T Create<T>(Type type)
        => (T)(Activator.CreateInstance(type) ?? throw new InvalidOperationException($"No se pudo crear {type.FullName}."));

    /// <summary>
    /// Envoltorios por tipo de petición, uno por resultado.
    /// </summary>
    /// <typeparam name="TResult">Resultado.</typeparam>
    private static class Wrappers<TResult>
        where TResult : Result
    {
        // Una caché por clase de envoltorio, con el tipo como clave: más barata que una tupla (definición, tipo).
        private static readonly ConcurrentDictionary<Type, RequestWrapper<TResult>> Commands = new ();
        private static readonly ConcurrentDictionary<Type, RequestWrapper<TResult>> Queries = new ();

        public static RequestWrapper<TResult> Command(Type request)
            => Commands.GetOrAdd(request, static type => Create<RequestWrapper<TResult>>(typeof(CommandWrapper<,>).MakeGenericType(type, typeof(TResult))));

        public static RequestWrapper<TResult> Query(Type request)
            => Queries.GetOrAdd(request, static type => Create<RequestWrapper<TResult>>(typeof(QueryWrapper<,>).MakeGenericType(type, typeof(TResult))));
    }
}
