// -----------------------------------------------------------------------
// <copyright file="NotificationWrapperOfT.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Envoltorio de una notificación concreta: sus manejadores uno detrás de otro, en orden de registro, parando en el
/// primer fallo. Una excepción se registra con el tipo del manejador.
/// </summary>
/// <typeparam name="TNotification">Notificación.</typeparam>
internal sealed class NotificationWrapper<TNotification> : NotificationWrapper
    where TNotification : INotification
{
    /// <inheritdoc/>
    public override async Task<Result> PublishAsync(INotification notification, IServiceProvider services, ILogger logger, CancellationToken cancellationToken)
    {
        var typed = (TNotification)notification;

        foreach (var handler in services.GetServices<INotificationHandler<TNotification>>())
        {
            Result result;

            try
            {
                var task = handler.HandleAsync(typed, cancellationToken)
                    ?? throw new InvalidOperationException($"{handler.GetType().FullName} devolvió una tarea nula.");

                result = await task.ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"{handler.GetType().FullName} devolvió un resultado nulo.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
#pragma warning disable CA1031 // Lo inesperado de un manejador vuelve como Result, como en los comandos y consultas.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                return Pipeline.Unexpected<Result>(logger, exception, typeof(TNotification), handler.GetType());
            }

            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }
}
