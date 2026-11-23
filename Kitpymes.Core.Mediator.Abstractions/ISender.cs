// -----------------------------------------------------------------------
// <copyright file="ISender.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;

/// <summary>
/// Envía comandos y consultas a su manejador. Es lo único que usa la capa API.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Envía un comando. Nunca lanza por un fallo del manejador: lo inesperado vuelve como <see cref="Result"/> de error.
    /// </summary>
    /// <typeparam name="TResult">Tipo del resultado: <see cref="Result"/> o <see cref="Result{T}"/>.</typeparam>
    /// <param name="command">Comando.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        where TResult : Result;

    /// <summary>
    /// Envía una consulta. Nunca lanza por un fallo del manejador: lo inesperado vuelve como <see cref="Result"/> de error.
    /// </summary>
    /// <typeparam name="TResult">Tipo del resultado: <see cref="Result"/> o <see cref="Result{T}"/>.</typeparam>
    /// <param name="query">Consulta.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        where TResult : Result;
}
