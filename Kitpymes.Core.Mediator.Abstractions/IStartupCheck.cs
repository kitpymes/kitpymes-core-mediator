// -----------------------------------------------------------------------
// <copyright file="IStartupCheck.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;

/// <summary>
/// Comprobación que se ejecuta al arrancar y en los tests de arquitectura (<c>MediatorVerifier.VerifyAsync</c>).
/// Si devuelve un fallo, la aplicación no arranca.
/// </summary>
public interface IStartupCheck
{
    /// <summary>
    /// Ejecuta la comprobación.
    /// </summary>
    /// <param name="catalog">Comandos, consultas y notificaciones registrados.</param>
    /// <param name="services">Proveedor de servicios de un ámbito creado para la comprobación.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Éxito, o un fallo con los problemas encontrados.</returns>
    Task<Result> CheckAsync(IMediatorCatalog catalog, IServiceProvider services, CancellationToken cancellationToken);
}
