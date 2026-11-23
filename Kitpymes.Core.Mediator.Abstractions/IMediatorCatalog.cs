// -----------------------------------------------------------------------
// <copyright file="IMediatorCatalog.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Abstractions;

using System;
using System.Collections.Generic;

/// <summary>
/// Lo que el mediador encontró al registrar los ensamblados. Lo usan las comprobaciones de arranque.
/// </summary>
public interface IMediatorCatalog
{
    /// <summary>
    /// Obtiene los tipos de comando encontrados.
    /// </summary>
    IReadOnlyCollection<Type> Commands { get; }

    /// <summary>
    /// Obtiene los tipos de consulta encontrados.
    /// </summary>
    IReadOnlyCollection<Type> Queries { get; }

    /// <summary>
    /// Obtiene los tipos de notificación encontrados.
    /// </summary>
    IReadOnlyCollection<Type> Notifications { get; }
}
