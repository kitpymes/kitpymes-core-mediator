// -----------------------------------------------------------------------
// <copyright file="IQuery.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using Kitpymes.Core.Results;

/// <summary>
/// Petición que sólo lee. No guarda nada.
/// </summary>
/// <typeparam name="TResult"><see cref="Result"/> o <see cref="Result{T}"/>.</typeparam>
public interface IQuery<TResult>
    where TResult : Result
{
}
