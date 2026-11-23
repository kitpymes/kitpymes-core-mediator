// -----------------------------------------------------------------------
// <copyright file="NotificationWrapper.cs" company="Kitpymes">
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
/// Envoltorio sin tipo de notificación: se crea una vez por tipo y se guarda en caché.
/// </summary>
internal abstract class NotificationWrapper
{
    /// <summary>
    /// Publica la notificación.
    /// </summary>
    /// <param name="notification">Notificación.</param>
    /// <param name="services">Proveedor del ámbito actual.</param>
    /// <param name="logger">Log.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    public abstract Task<Result> PublishAsync(INotification notification, IServiceProvider services, ILogger logger, CancellationToken cancellationToken);
}
