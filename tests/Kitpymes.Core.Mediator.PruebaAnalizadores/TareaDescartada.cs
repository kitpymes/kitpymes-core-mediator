// -----------------------------------------------------------------------
// <copyright file="TareaDescartada.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.PruebaAnalizadores;

using System.Threading.Tasks;

/// <summary>
/// Código que VSTHRD110 tiene que marcar como error cuando se compila con DESCARTE_OLVIDADO.
/// </summary>
public static class TareaDescartada
{
    /// <summary>
    /// Lanza una tarea desde un método síncrono.
    /// </summary>
    public static void Lanzar()
    {
#if DESCARTE_OLVIDADO
        Task.Delay(1);
#else
        _ = Task.Delay(1);
#endif
    }
}
