// -----------------------------------------------------------------------
// <copyright file="IPublisher.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;

/// <summary>
/// Publica notificaciones.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publica una notificación a sus manejadores, uno detrás de otro. Para en el primero que falla y devuelve ese error.
    /// </summary>
    /// <remarks>
    /// Sólo se ejecutan los manejadores del tipo exacto de la notificación: publicar un evento derivado no ejecuta los
    /// manejadores de su clase base. Los manejadores van en orden alfabético de su nombre completo.
    /// </remarks>
    /// <param name="notification">Notificación.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    Task<Result> PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}
