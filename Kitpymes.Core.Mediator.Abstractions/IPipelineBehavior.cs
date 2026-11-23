// -----------------------------------------------------------------------
// <copyright file="IPipelineBehavior.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;

/// <summary>
/// Comportamiento que envuelve a todos los comandos y consultas.
/// </summary>
/// <typeparam name="TRequest">Comando o consulta.</typeparam>
/// <typeparam name="TResult">Resultado.</typeparam>
/// <remarks>
/// <para>Se registra como genérico abierto: <c>services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(MiComportamiento&lt;,&gt;))</c>.</para>
/// <para>Orden: la captura de lo inesperado va por fuera de todo; después los comportamientos generales y, por dentro,
/// los de comando o consulta, cada grupo en orden de registro. Un comportamiento llama a <c>next()</c> una sola vez:
/// nada impide llamarlo dos veces (el manejador se ejecutaría dos veces) o ninguna.</para>
/// </remarks>
public interface IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
    where TResult : Result
{
    /// <summary>
    /// Ejecuta el comportamiento.
    /// </summary>
    /// <param name="request">Comando o consulta.</param>
    /// <param name="next">Siguiente paso.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    Task<TResult> HandleAsync(TRequest request, NextHandler<TResult> next, CancellationToken cancellationToken);
}
