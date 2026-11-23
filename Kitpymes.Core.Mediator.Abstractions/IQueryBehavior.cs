// -----------------------------------------------------------------------
// <copyright file="IQueryBehavior.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;

/// <summary>
/// Comportamiento que sólo envuelve a las consultas (p. ej. una caché).
/// </summary>
/// <typeparam name="TQuery">Consulta.</typeparam>
/// <typeparam name="TResult">Resultado.</typeparam>
public interface IQueryBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : Result
{
    /// <summary>
    /// Ejecuta el comportamiento.
    /// </summary>
    /// <param name="query">Consulta.</param>
    /// <param name="next">Siguiente paso.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    Task<TResult> HandleAsync(TQuery query, NextHandler<TResult> next, CancellationToken cancellationToken);
}
