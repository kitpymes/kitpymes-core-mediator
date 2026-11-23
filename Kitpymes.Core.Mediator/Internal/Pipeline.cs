// -----------------------------------------------------------------------
// <copyright file="Pipeline.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Results;
using Microsoft.Extensions.Logging;

/// <summary>
/// Captura común: lo inesperado se registra (tipo de la petición y excepción completa, nunca la petición)
/// y vuelve como <see cref="Result"/> de error.
/// </summary>
internal static class Pipeline
{
    /// <summary>
    /// Ejecuta algo dentro de la captura común.
    /// </summary>
    /// <typeparam name="TResult">Tipo del resultado.</typeparam>
    /// <param name="requestType">Tipo de la petición o notificación (lo único que se registra de ella).</param>
    /// <param name="handlerType">Tipo del manejador que falló, si se sabe.</param>
    /// <param name="logger">Log.</param>
    /// <param name="run">Lo que se ejecuta. Se llama dentro de la captura: también se captura lo que lance antes de devolver la tarea.</param>
    /// <param name="cancellationToken">Cancelación de quien llama.</param>
    /// <returns>Resultado.</returns>
    public static async Task<TResult> RunAsync<TResult>(Type requestType, Type? handlerType, ILogger logger, Func<Task<TResult>> run, CancellationToken cancellationToken)
        where TResult : Result
    {
        try
        {
            var task = run() ?? throw new InvalidOperationException($"{handlerType?.FullName ?? requestType.FullName} devolvió una tarea nula.");

            var result = await task.ConfigureAwait(false);

            return result ?? throw new InvalidOperationException($"{handlerType?.FullName ?? requestType.FullName} devolvió un resultado nulo.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Quien llama pidió cancelar: no espera respuesta. Se mira el token, no el de la excepción,
            // porque con tokens enlazados no son el mismo.
            throw;
        }
        catch (Exception exception)
        {
            Log(logger, exception, requestType, handlerType);

            return Result.FailureFor<TResult>(Error.Unexpected(MediatorErrors.Unexpected, MediatorErrors.UnexpectedMessage));
        }
    }

    private static void Log(ILogger logger, Exception exception, Type requestType, Type? handlerType)
    {
        try
        {
            if (handlerType is null)
            {
                logger.LogError(exception, "Error inesperado al ejecutar {RequestType}.", requestType.FullName);
            }
            else
            {
                logger.LogError(exception, "Error inesperado al ejecutar {RequestType} en {HandlerType}.", requestType.FullName, handlerType.FullName);
            }
        }
#pragma warning disable CA1031 // Si el propio log falla, quien llama tiene que recibir igualmente su Result.
        catch (Exception)
#pragma warning restore CA1031
        {
            // Nada más que hacer: no hay dónde registrarlo.
        }
    }
}
