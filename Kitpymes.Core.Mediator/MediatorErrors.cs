// -----------------------------------------------------------------------
// <copyright file="MediatorErrors.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator;

/// <summary>
/// Códigos de error que devuelve el mediador.
/// </summary>
public static class MediatorErrors
{
    /// <summary>
    /// Fallo inesperado capturado por el mediador (excepción de un manejador o comportamiento).
    /// </summary>
    public const string Unexpected = "Core.Unexpected";

    /// <summary>
    /// El mediador no está bien configurado (lo devuelve <see cref="MediatorVerifier.VerifyAsync"/>).
    /// </summary>
    public const string Configuration = "Core.MediatorConfiguracion";

    /// <summary>
    /// Obtiene el mensaje que recibe quien llama cuando algo inesperado falla. No lleva detalles internos.
    /// </summary>
    /// <remarks>Es una propiedad y no una constante: así se puede cambiar el texto sin que quien lo use tenga que recompilar.</remarks>
    public static string UnexpectedMessage => "Se ha producido un error inesperado.";
}
