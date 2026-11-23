// -----------------------------------------------------------------------
// <copyright file="RequestWrapper.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Envoltorio de una petición para un resultado: se crea una vez por tipo de petición y se guarda en caché.
/// </summary>
/// <typeparam name="TResult">Resultado.</typeparam>
internal abstract class RequestWrapper<TResult>
    where TResult : Result
{
    /// <summary>
    /// Ejecuta la petición. Se llama dentro de la captura común.
    /// </summary>
    /// <param name="request">Petición.</param>
    /// <param name="services">Proveedor del ámbito actual.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    public abstract Task<TResult> HandleAsync(object request, IServiceProvider services, CancellationToken cancellationToken);

    /// <summary>
    /// Encadena los comportamientos alrededor del manejador: los generales por fuera y los específicos por dentro,
    /// cada grupo en orden de registro.
    /// </summary>
    /// <typeparam name="TRequest">Petición.</typeparam>
    /// <param name="request">Petición.</param>
    /// <param name="handler">Manejador.</param>
    /// <param name="specific">Comportamientos de comando o de consulta.</param>
    /// <param name="general">Comportamientos para todo.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    protected static Task<TResult> RunAsync<TRequest>(
        TRequest request,
        NextHandler<TResult> handler,
        Func<TRequest, NextHandler<TResult>, CancellationToken, Task<TResult>>[] specific,
        IPipelineBehavior<TRequest, TResult>[] general,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var next = handler;

        for (var i = specific.Length - 1; i >= 0; i--)
        {
            var behavior = specific[i];
            var inner = next;
            next = () => behavior(request, inner, cancellationToken);
        }

        for (var i = general.Length - 1; i >= 0; i--)
        {
            var behavior = general[i];
            var inner = next;
            next = () => behavior.HandleAsync(request, inner, cancellationToken);
        }

        return next();
    }
}
