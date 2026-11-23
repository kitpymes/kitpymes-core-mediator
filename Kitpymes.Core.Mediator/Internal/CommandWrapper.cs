// -----------------------------------------------------------------------
// <copyright file="CommandWrapper.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.Mediator.Internal;

using System;
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

        return RunAsync(
            command,
            () => handler.HandleAsync(command, cancellationToken),
            services.GetServices<ICommandBehavior<TCommand, TResult>>().Select(b => (Func<TCommand, NextHandler<TResult>, CancellationToken, Task<TResult>>)b.HandleAsync).ToArray(),
            services.GetServices<IPipelineBehavior<TCommand, TResult>>().ToArray(),
            cancellationToken);
    }
}
