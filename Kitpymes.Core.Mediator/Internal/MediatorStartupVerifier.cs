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
/// Ejecuta <see cref="MediatorVerifier.VerifyAsync(IServiceProvider, CancellationToken)"/> al arrancar el host: si falla, la aplicación no arranca.
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
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await MediatorVerifier.VerifyAsync(_services, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Message);
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
