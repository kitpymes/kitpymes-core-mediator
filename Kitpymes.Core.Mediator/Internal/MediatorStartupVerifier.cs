// -----------------------------------------------------------------------
// <copyright file="MediatorStartupVerifier.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Ejecuta <see cref="MediatorVerifier.Verify(IServiceProvider)"/> al arrancar el host: si falla, la aplicación no arranca.
/// </summary>
internal sealed class MediatorStartupVerifier : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="MediatorStartupVerifier"/>.
    /// </summary>
    /// <param name="services">Proveedor raíz.</param>
    public MediatorStartupVerifier(IServiceProvider services) => _services = services;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var result = MediatorVerifier.Verify(_services);

        return result.IsFailure
            ? throw new InvalidOperationException(result.Error.Message)
            : Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
