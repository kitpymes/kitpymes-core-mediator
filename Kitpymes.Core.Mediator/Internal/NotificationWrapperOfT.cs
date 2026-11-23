// -----------------------------------------------------------------------
// <copyright file="NotificationWrapperOfT.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Envoltorio de una notificación concreta: sus manejadores uno detrás de otro, en orden alfabético de su nombre
/// completo, parando en el primer fallo.
/// </summary>
/// <typeparam name="TNotification">Notificación.</typeparam>
internal sealed class NotificationWrapper<TNotification> : NotificationWrapper
    where TNotification : INotification
{
    /// <inheritdoc/>
    public override async Task<Result> PublishAsync(INotification notification, IServiceProvider services, ILogger logger, CancellationToken cancellationToken)
    {
        var typed = (TNotification)notification;

        var handlers = services.GetServices<INotificationHandler<TNotification>>()
            .OrderBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToArray();

        foreach (var handler in handlers)
        {
            var result = await Pipeline.RunAsync(typeof(TNotification), handler.GetType(), logger, () => handler.HandleAsync(typed, cancellationToken), cancellationToken).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }
}
