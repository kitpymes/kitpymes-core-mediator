// -----------------------------------------------------------------------
// <copyright file="INotificationHandler.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;

/// <summary>
/// Manejador de una notificación. Una notificación puede tener cero o más.
/// </summary>
/// <typeparam name="TNotification">Notificación.</typeparam>
public interface INotificationHandler<TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Atiende la notificación. Si devuelve un fallo, la publicación se detiene.
    /// </summary>
    /// <param name="notification">Notificación.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    Task<Result> HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
