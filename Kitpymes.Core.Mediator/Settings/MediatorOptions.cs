// -----------------------------------------------------------------------
// <copyright file="MediatorOptions.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Settings;

using System;
using System.Reflection;

/// <summary>
/// Opciones para configurar el mediador.
/// </summary>
public class MediatorOptions
{
    /// <summary>
    /// Obtiene la configuración.
    /// </summary>
    public MediatorSettings MediatorSettings { get; } = new ();

    /// <summary>
    /// Agrega ensamblados donde buscar comandos, consultas, notificaciones y manejadores.
    /// </summary>
    /// <param name="assemblies">Ensamblados.</param>
    /// <returns>MediatorOptions.</returns>
    public MediatorOptions WithAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            ArgumentNullException.ThrowIfNull(assembly, nameof(assemblies));

            if (!MediatorSettings.Assemblies.Contains(assembly))
            {
                MediatorSettings.Assemblies.Add(assembly);
            }
        }

        return this;
    }

    /// <summary>
    /// Exige (o no) la comprobación de validación al arrancar. Quitarla sólo tiene sentido en tests o librerías.
    /// </summary>
    /// <param name="enabled">Si se exige. Por defecto, sí.</param>
    /// <returns>MediatorOptions.</returns>
    public MediatorOptions WithValidation(bool enabled = true)
    {
        MediatorSettings.Validation = enabled;

        return this;
    }

    /// <summary>
    /// Comprueba (o no) la configuración al arrancar el host. Quitarla sólo tiene sentido en tests de integración
    /// (p. ej. con <c>WebApplicationFactory</c>), que deben llamar ellos a <c>MediatorVerifier.Verify</c>.
    /// </summary>
    /// <param name="enabled">Si se comprueba. Por defecto, sí.</param>
    /// <returns>MediatorOptions.</returns>
    public MediatorOptions WithStartupVerification(bool enabled = true)
    {
        MediatorSettings.StartupVerification = enabled;

        return this;
    }
}
