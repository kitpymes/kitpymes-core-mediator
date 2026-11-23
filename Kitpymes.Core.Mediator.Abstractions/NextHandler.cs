// -----------------------------------------------------------------------
// <copyright file="NextHandler.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System.Threading.Tasks;
using Kitpymes.Core.Results;

/// <summary>
/// Siguiente paso de la tubería: otro comportamiento o el manejador.
/// </summary>
/// <typeparam name="TResult">Resultado.</typeparam>
/// <returns>Resultado.</returns>
public delegate Task<TResult> NextHandler<TResult>()
    where TResult : Result;
