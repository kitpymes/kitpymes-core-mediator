// -----------------------------------------------------------------------
// <copyright file="ICommand.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using Kitpymes.Core.Results;

/// <summary>
/// Petición que modifica el estado. Sólo los comandos abren transacción.
/// </summary>
/// <typeparam name="TResult"><see cref="Result"/> o <see cref="Result{T}"/>.</typeparam>
/// <remarks>Una clase no puede ser a la vez comando y consulta: lo comprueba <c>MediatorVerifier</c>.</remarks>
public interface ICommand<TResult>
    where TResult : Result
{
}
