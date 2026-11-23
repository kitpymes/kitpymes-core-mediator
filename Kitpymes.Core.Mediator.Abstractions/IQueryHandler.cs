// -----------------------------------------------------------------------
// <copyright file="IQueryHandler.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;

/// <summary>
/// Manejador de una consulta. Cada consulta tiene exactamente uno.
/// </summary>
/// <typeparam name="TQuery">Consulta.</typeparam>
/// <typeparam name="TResult">Resultado.</typeparam>
public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : Result
{
    /// <summary>
    /// Ejecuta la consulta.
    /// </summary>
    /// <param name="query">Consulta.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
