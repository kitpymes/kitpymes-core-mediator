// -----------------------------------------------------------------------
// <copyright file="QueryWrapper.cs" company="Kitpymes">
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
        var general = ToArray(services.GetServices<IPipelineBehavior<TQuery, TResult>>());
        var specific = ToArray(services.GetServices<IQueryBehavior<TQuery, TResult>>());

        if (general.Length == 0 && specific.Length == 0)
        {
            return handler.HandleAsync(query, cancellationToken);
        }

        NextHandler<TResult> next = () => handler.HandleAsync(query, cancellationToken);

        for (var i = specific.Length - 1; i >= 0; i--)
        {
            var behavior = specific[i];
            var inner = next;
            next = () => behavior.HandleAsync(query, inner, cancellationToken);
        }

        for (var i = general.Length - 1; i >= 0; i--)
        {
            var behavior = general[i];
            var inner = next;
            next = () => behavior.HandleAsync(query, inner, cancellationToken);
        }

        return next();
    }

    // MS DI devuelve un array: se evita copiarlo.
    private static T[] ToArray<T>(IEnumerable<T> items) => items as T[] ?? items.ToArray();
}
