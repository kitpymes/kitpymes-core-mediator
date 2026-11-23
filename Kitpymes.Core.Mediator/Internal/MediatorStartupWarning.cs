// -----------------------------------------------------------------------
// <copyright file="MediatorStartupWarning.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Con <c>WithStartupVerification(false)</c>, avisa en el log al arrancar de que la configuración del mediador no se ha
/// comprobado. Sólo tiene sentido en tests de integración.
/// </summary>
internal sealed class MediatorStartupWarning : IHostedService
{
    private readonly MediatorCatalog _catalog;
    private readonly ILogger<MediatorStartupWarning> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="MediatorStartupWarning"/>.
    /// </summary>
    /// <param name="catalog">Catálogo del mediador.</param>
    /// <param name="logger">Log.</param>
    public MediatorStartupWarning(MediatorCatalog catalog, ILogger<MediatorStartupWarning> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Si otra llamada a LoadMediator pidió la comprobación, la ejecuta MediatorStartupVerifier y no hay nada que avisar.
        if (!_catalog.VerifyAtStartup)
        {
            _logger.LogWarning("La configuración del mediador no se ha comprobado al arrancar (WithStartupVerification(false)). Sólo debe usarse en tests de integración, que llaman ellos a MediatorVerifier.VerifyAsync.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
