// -----------------------------------------------------------------------
// <copyright file="CommandWrapper.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Envoltorio de un comando concreto.
/// </summary>
/// <typeparam name="TCommand">Comando.</typeparam>
/// <typeparam name="TResult">Resultado.</typeparam>
internal sealed class CommandWrapper<TCommand, TResult> : RequestWrapper<TResult>
    where TCommand : ICommand<TResult>
    where TResult : Result
{
    /// <inheritdoc/>
    public override Task<TResult> HandleAsync(object request, IServiceProvider services, CancellationToken cancellationToken)
    {
        var command = (TCommand)request;
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        var general = ToArray(services.GetServices<IPipelineBehavior<TCommand, TResult>>());
        var specific = ToArray(services.GetServices<ICommandBehavior<TCommand, TResult>>());

        if (general.Length == 0 && specific.Length == 0)
        {
            return handler.HandleAsync(command, cancellationToken);
        }

        NextHandler<TResult> next = () => handler.HandleAsync(command, cancellationToken);

        for (var i = specific.Length - 1; i >= 0; i--)
        {
            var behavior = specific[i];
            var inner = next;
            next = () => behavior.HandleAsync(command, inner, cancellationToken);
        }

        for (var i = general.Length - 1; i >= 0; i--)
        {
            var behavior = general[i];
            var inner = next;
            next = () => behavior.HandleAsync(command, inner, cancellationToken);
        }

        return next();
    }

    // MS DI devuelve un array: se evita copiarlo.
    private static T[] ToArray<T>(IEnumerable<T> items) => items as T[] ?? items.ToArray();
}
