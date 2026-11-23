// -----------------------------------------------------------------------
// <copyright file="MediatorSettings.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Settings;

using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Configuración del mediador.
/// </summary>
public class MediatorSettings
{
    /// <summary>
    /// Obtiene los ensamblados donde se buscan comandos, consultas, notificaciones y sus manejadores.
    /// </summary>
    public IList<Assembly> Assemblies { get; } = new List<Assembly>();

    /// <summary>
    /// Obtiene o establece un valor que indica si se exige la comprobación de validación al arrancar. Por defecto, sí.
    /// </summary>
    public bool Validation { get; set; } = true;

    /// <summary>
    /// Obtiene o establece un valor que indica si se comprueba la configuración al arrancar el host. Por defecto, sí.
    /// </summary>
    public bool StartupVerification { get; set; } = true;
}
