// -----------------------------------------------------------------------
// <copyright file="Pipeline.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Runtime.ExceptionServices;
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
    /// <param name="logger">Log.</param>
    /// <param name="run">Lo que se ejecuta. Se llama dentro de la captura: también se captura lo que lance antes de devolver la tarea.</param>
    /// <param name="cancellationToken">Cancelación de quien llama.</param>
    /// <returns>Resultado.</returns>
    public static Task<TResult> RunAsync<TResult>(Type requestType, ILogger logger, Func<Task<TResult>> run, CancellationToken cancellationToken)
        where TResult : Result
    {
        Task<TResult>? task = null;
        Exception? error = null;

        try
        {
            task = run();
        }
#pragma warning disable CA1031 // Se relanza dentro de CompleteAsync, con la misma captura que si hubiera fallado la tarea.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            error = exception;
        }

        return Complete(task, error, requestType, logger, cancellationToken);
    }

    /// <summary>
    /// Termina dentro de la captura común lo que ya se ha ejecutado.
    /// </summary>
    /// <remarks>
    /// Camino rápido: si la tarea ya terminó bien y con resultado, se devuelve tal cual, sin máquina de estados ni otra tarea.
    /// Lo demás (excepción al llamar, tarea nula, resultado nulo, fallo o tarea pendiente) pasa por <see cref="CompleteAsync"/>,
    /// que se comporta igual que si todo hubiera ido en un único método asíncrono.
    /// </remarks>
    /// <typeparam name="TResult">Tipo del resultado.</typeparam>
    /// <param name="task">Tarea devuelta, o nula si no se llegó a devolver.</param>
    /// <param name="error">Excepción lanzada antes de devolver la tarea, si la hubo.</param>
    /// <param name="requestType">Tipo de la petición o notificación (lo único que se registra de ella).</param>
    /// <param name="logger">Log.</param>
    /// <param name="cancellationToken">Cancelación de quien llama.</param>
    /// <returns>Resultado.</returns>
    public static Task<TResult> Complete<TResult>(Task<TResult>? task, Exception? error, Type requestType, ILogger logger, CancellationToken cancellationToken)
        where TResult : Result
    {
        if (error is null && task is not null && task.IsCompletedSuccessfully && task.Result is not null)
        {
            return task;
        }

        return CompleteAsync(task, error, requestType, logger, cancellationToken);
    }

    /// <summary>
    /// Registra lo inesperado y devuelve el <see cref="Result"/> de error que recibe quien llama.
    /// </summary>
    /// <typeparam name="TResult">Tipo del resultado.</typeparam>
    /// <param name="logger">Log.</param>
    /// <param name="exception">Excepción.</param>
    /// <param name="requestType">Tipo de la petición o notificación.</param>
    /// <param name="handlerType">Tipo del manejador, si se sabe.</param>
    /// <returns>Fallo <see cref="MediatorErrors.Unexpected"/>.</returns>
    public static TResult Unexpected<TResult>(ILogger logger, Exception exception, Type requestType, Type? handlerType)
        where TResult : Result
    {
        Log(logger, exception, requestType, handlerType);

        return Result.FailureFor<TResult>(Error.Unexpected(MediatorErrors.Unexpected, MediatorErrors.UnexpectedMessage));
    }

    private static async Task<TResult> CompleteAsync<TResult>(Task<TResult>? task, Exception? error, Type requestType, ILogger logger, CancellationToken cancellationToken)
        where TResult : Result
    {
        try
        {
            if (error is not null)
            {
                // Se relanza aquí dentro para que la cancelación y la captura sean las mismas que con una tarea que falla.
                ExceptionDispatchInfo.Throw(error);
            }

            var pending = task ?? throw new InvalidOperationException($"La tubería de {requestType.FullName} devolvió una tarea nula.");

            var result = await pending.ConfigureAwait(false);

            return result ?? throw new InvalidOperationException($"La tubería de {requestType.FullName} devolvió un resultado nulo.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Quien llama pidió cancelar: no espera respuesta. Se mira el token, no el de la excepción,
            // porque con tokens enlazados no son el mismo.
            throw;
        }
        catch (Exception exception)
        {
            return Unexpected<TResult>(logger, exception, requestType, null);
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
