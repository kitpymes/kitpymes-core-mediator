// -----------------------------------------------------------------------
// <copyright file="RequestWrapper.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;

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
    /// <remarks>
    /// Orden: los comportamientos generales por fuera y los específicos (de comando o de consulta) por dentro, cada grupo
    /// en orden de registro. Sin comportamientos se llama directamente al manejador.
    /// </remarks>
    /// <param name="request">Petición.</param>
    /// <param name="services">Proveedor del ámbito actual.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Resultado.</returns>
    public abstract Task<TResult> HandleAsync(object request, IServiceProvider services, CancellationToken cancellationToken);
}
