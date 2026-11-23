// -----------------------------------------------------------------------
// <copyright file="IValidationStartupCheck.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

/// <summary>
/// Comprobación de arranque de la validación: que el comportamiento de validación está registrado y que todo
/// comando y consulta con propiedades tiene su validador. La aporta la librería de validación.
/// </summary>
/// <remarks>Por defecto el mediador exige que haya una registrada; se quita con <c>WithValidation(false)</c>.</remarks>
public interface IValidationStartupCheck : IStartupCheck
{
}
