// -----------------------------------------------------------------------
// <copyright file="QueryWrapper.cs" company="Kitpymes">
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
/// Envoltorio de una consulta concreta.
/// </summary>
/// <typeparam name="TQuery">Consulta.</typeparam>
/// <typeparam name="TResult">Resultado.</typeparam>
internal sealed class QueryWrapper<TQuery, TResult> : RequestWrapper<TResult>
    where TQuery : IQuery<TResult>
    where TResult : Result
{
    /// <inheritdoc/>
    public override Task<TResult> HandleAsync(object request, IServiceProvider services, CancellationToken cancellationToken)
    {
        var query = (TQuery)request;
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResult>>();

        return RunAsync(
            query,
            () => handler.HandleAsync(query, cancellationToken),
            services.GetServices<IQueryBehavior<TQuery, TResult>>().Select(b => (Func<TQuery, NextHandler<TResult>, CancellationToken, Task<TResult>>)b.HandleAsync).ToArray(),
            services.GetServices<IPipelineBehavior<TQuery, TResult>>().ToArray(),
            cancellationToken);
    }
}
