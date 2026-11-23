// -----------------------------------------------------------------------
// <copyright file="MediatorCatalog.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kitpymes.Core.Mediator.Abstractions;

/// <summary>
/// Lo que se encontró al registrar los ensamblados: peticiones, notificaciones y sus manejadores.
/// </summary>
internal sealed class MediatorCatalog : IMediatorCatalog
{
    private readonly HashSet<Assembly> _assemblies = new ();
    private readonly HashSet<Type> _commands = new ();
    private readonly HashSet<Type> _queries = new ();
    private readonly HashSet<Type> _notifications = new ();
    private readonly HashSet<Type> _handlerServices = new ();

    /// <inheritdoc/>
    public IReadOnlyCollection<Type> Commands => _commands;

    /// <inheritdoc/>
    public IReadOnlyCollection<Type> Queries => _queries;

    /// <inheritdoc/>
    public IReadOnlyCollection<Type> Notifications => _notifications;

    /// <summary>
    /// Obtiene los ensamblados registrados.
    /// </summary>
    public IReadOnlyCollection<Assembly> Assemblies => _assemblies;

    /// <summary>
    /// Obtiene los tipos de servicio de manejador registrados (p. ej. <c>ICommandHandler&lt;X, Result&gt;</c>).
    /// </summary>
    public IReadOnlyCollection<Type> HandlerServices => _handlerServices;

    /// <summary>
    /// Obtiene o establece un valor que indica si se exige la comprobación de validación.
    /// </summary>
    public bool RequireValidation { get; set; } = true;

    /// <summary>
    /// Nombre legible de un tipo, con sus argumentos genéricos.
    /// </summary>
    /// <param name="type">Tipo.</param>
    /// <returns>Nombre.</returns>
    public static string Name(Type type)
        => type.IsGenericType
            ? $"{type.Name.Substring(0, type.Name.IndexOf('`', StringComparison.Ordinal))}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>"
            : type.Name;

    /// <summary>
    /// Agrega un ensamblado si no estaba.
    /// </summary>
    /// <param name="assembly">Ensamblado.</param>
    /// <returns><c>true</c> si es nuevo.</returns>
    public bool AddAssembly(Assembly assembly) => _assemblies.Add(assembly);

    /// <summary>
    /// Registra una petición o notificación encontrada.
    /// </summary>
    /// <param name="type">Tipo.</param>
    public void AddMessage(Type type)
    {
        foreach (var contract in type.GetInterfaces())
        {
            if (contract == typeof(INotification))
            {
                _notifications.Add(type);
            }
            else if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ICommand<>))
            {
                _commands.Add(type);
            }
            else if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IQuery<>))
            {
                _queries.Add(type);
            }
        }
    }

    /// <summary>
    /// Registra un manejador de una petición o notificación.
    /// </summary>
    /// <param name="message">Petición o notificación que maneja.</param>
    /// <param name="service">Tipo de servicio (interfaz cerrada).</param>
    public void AddHandler(Type message, Type service)
    {
        _handlerServices.Add(service);

        // La petición puede vivir en un ensamblado que no se escanea (p. ej. Contracts): se toma también del manejador.
        AddMessage(message);
    }

    /// <summary>
    /// Devuelve los tipos de petición que se han registrado como comando y como consulta, o como comando o consulta con dos resultados.
    /// </summary>
    /// <returns>Tipos mal declarados con el motivo.</returns>
    public IEnumerable<string> InvalidRequests()
    {
        foreach (var type in _commands.Concat(_queries).Distinct().OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            var contracts = type.GetInterfaces()
                .Where(x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(ICommand<>) || x.GetGenericTypeDefinition() == typeof(IQuery<>)))
                .ToList();

            if (contracts.Count > 1)
            {
                yield return $"{type.FullName} implementa a la vez {string.Join(" y ", contracts.Select(Name))}: una petición es un comando o una consulta, con un solo resultado.";
            }
        }
    }
}
