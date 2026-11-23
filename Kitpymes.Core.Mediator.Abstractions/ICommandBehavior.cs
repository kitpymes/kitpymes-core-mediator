// -----------------------------------------------------------------------
// <copyright file="ICommandBehavior.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;

/// <summary>
/// Comportamiento que sólo envuelve a los comandos (p. ej. la transacción).
/// </summary>
/// <typeparam name="TCommand">Comando.</typeparam>
/// <typeparam name="TResult">Resultado.</typeparam>
public interface ICommandBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
    where TResult : Result
{
    /// <summary>
    /// Ejecuta el comportamiento.
    /// </summary>
    /// <param name="command">Comando.</param>
    /// <param name="next">Siguiente paso.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    Task<TResult> HandleAsync(TCommand command, NextHandler<TResult> next, CancellationToken cancellationToken);
}
