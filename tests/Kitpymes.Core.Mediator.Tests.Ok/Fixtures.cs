// Tipos de prueba con una configuración correcta: los escanea Kitpymes.Core.Mediator.Tests.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Results;

namespace Kitpymes.Core.Mediator.Tests.Ok
{
    /// <summary>Registro compartido de lo que se ejecuta, en orden.</summary>
    public sealed class Trace
    {
        public List<string> Steps { get; } = new ();
    }

    /// <summary>Qué hace el manejador de <see cref="DoCommand"/>.</summary>
    public enum Behavior { Ok, Fail, Throw, ThrowBeforeTask, NullTask, NullResult, Cancel }

    public sealed record DoCommand(Behavior Behavior, string Secret = "contraseña-secreta") : ICommand<Result>;

    public sealed class DoCommandHandler : ICommandHandler<DoCommand, Result>
    {
        private readonly Trace _trace;

        public DoCommandHandler(Trace trace) => _trace = trace;

        public Task<Result> HandleAsync(DoCommand command, CancellationToken cancellationToken)
        {
            _trace.Steps.Add("handler");

            return command.Behavior switch
            {
                Behavior.Ok => Task.FromResult(Result.Success()),
                Behavior.Fail => Task.FromResult(Result.Failure(Error.Conflict("Tests.Conflicto", "Conflicto."))),
                Behavior.Throw => ThrowAsync(),
                Behavior.ThrowBeforeTask => throw new InvalidOperationException("antes de la tarea"),
                Behavior.NullTask => null!,
                Behavior.NullResult => Task.FromResult<Result>(null!),
                _ => CancelAsync(cancellationToken),
            };
        }

        private static async Task<Result> ThrowAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("fallo del manejador");
        }

        private static async Task<Result> CancelAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("cancelación que no pidió quien llama");
        }
    }

    public sealed record GetNumberQuery(int Value) : IQuery<Result<int>>;

    public sealed class GetNumberQueryHandler : IQueryHandler<GetNumberQuery, Result<int>>
    {
        private readonly Trace _trace;

        public GetNumberQueryHandler(Trace trace) => _trace = trace;

        public Task<Result<int>> HandleAsync(GetNumberQuery query, CancellationToken cancellationToken)
        {
            _trace.Steps.Add("handler");
            return Task.FromResult(query.Value < 0 ? Result<int>.Failure(Error.Validation("Tests.Negativo", "Negativo.")) : Result.Success(query.Value * 2));
        }
    }

    public sealed record GetTextQuery : IQuery<Result<string>>;

    public sealed class GetTextQueryHandler : IQueryHandler<GetTextQuery, Result<string>>
    {
        public Task<Result<string>> HandleAsync(GetTextQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success("texto"));
    }

    public record SomethingHappened(string Name) : INotification;

    public sealed record SomethingDerivedHappened(string Name) : SomethingHappened(Name);

    // Los nombres fijan el orden alfabético: A, B, C.
    public sealed class AFirstHandler : INotificationHandler<SomethingHappened>
    {
        private readonly Trace _trace;

        public AFirstHandler(Trace trace) => _trace = trace;

        public Task<Result> HandleAsync(SomethingHappened notification, CancellationToken cancellationToken)
        {
            _trace.Steps.Add("A");
            return Task.FromResult(notification.Name == "falla-en-A" ? Result.Failure(Error.Conflict("Tests.FallaA", "Falla A.")) : Result.Success());
        }
    }

    public sealed class BSecondHandler : INotificationHandler<SomethingHappened>
    {
        private readonly Trace _trace;

        public BSecondHandler(Trace trace) => _trace = trace;

        public Task<Result> HandleAsync(SomethingHappened notification, CancellationToken cancellationToken)
        {
            _trace.Steps.Add("B");
            return notification.Name == "lanza-en-B" ? throw new InvalidOperationException("fallo en B") : Task.FromResult(Result.Success());
        }
    }

    public sealed class CThirdHandler : INotificationHandler<SomethingHappened>
    {
        private readonly Trace _trace;

        public CThirdHandler(Trace trace) => _trace = trace;

        public Task<Result> HandleAsync(SomethingHappened notification, CancellationToken cancellationToken)
        {
            _trace.Steps.Add("C");
            return Task.FromResult(notification.Name == "nulo-en-C" ? null! : Result.Success());
        }
    }

    public sealed record NobodyListens : INotification;

    // Una clase puede manejar varias cosas.
    public sealed class MultiHandler : ICommandHandler<PingCommand, Result<string>>, INotificationHandler<Pinged>
    {
        public Task<Result<string>> HandleAsync(PingCommand command, CancellationToken cancellationToken) => Task.FromResult(Result.Success("pong"));

        public Task<Result> HandleAsync(Pinged notification, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    public sealed record PingCommand : ICommand<Result<string>>;

    public sealed record Pinged : INotification;

    // Los genéricos abiertos de manejadores se ignoran al escanear.
    public sealed class OpenGenericHandler<T> : INotificationHandler<Pinged>
    {
        public Task<Result> HandleAsync(Pinged notification, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    // Comportamientos de prueba: dejan su nombre en la traza, antes y después.
    public sealed class GeneralBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
        where TRequest : notnull
        where TResult : Result
    {
        private readonly Trace _trace;

        public GeneralBehavior(Trace trace) => _trace = trace;

        public async Task<TResult> HandleAsync(TRequest request, NextHandler<TResult> next, CancellationToken cancellationToken)
        {
            _trace.Steps.Add("general>");
            var result = await next();
            _trace.Steps.Add("<general");
            return result;
        }
    }

    public sealed class SecondGeneralBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
        where TRequest : notnull
        where TResult : Result
    {
        private readonly Trace _trace;

        public SecondGeneralBehavior(Trace trace) => _trace = trace;

        public async Task<TResult> HandleAsync(TRequest request, NextHandler<TResult> next, CancellationToken cancellationToken)
        {
            _trace.Steps.Add("general2>");
            var result = await next();
            _trace.Steps.Add("<general2");
            return result;
        }
    }

    public sealed class CommandOnlyBehavior<TCommand, TResult> : ICommandBehavior<TCommand, TResult>
        where TCommand : ICommand<TResult>
        where TResult : Result
    {
        private readonly Trace _trace;

        public CommandOnlyBehavior(Trace trace) => _trace = trace;

        public async Task<TResult> HandleAsync(TCommand command, NextHandler<TResult> next, CancellationToken cancellationToken)
        {
            _trace.Steps.Add("command>");
            var result = await next();
            _trace.Steps.Add("<command");
            return result;
        }
    }

    public sealed class QueryOnlyBehavior<TQuery, TResult> : IQueryBehavior<TQuery, TResult>
        where TQuery : IQuery<TResult>
        where TResult : Result
    {
        private readonly Trace _trace;

        public QueryOnlyBehavior(Trace trace) => _trace = trace;

        public async Task<TResult> HandleAsync(TQuery query, NextHandler<TResult> next, CancellationToken cancellationToken)
        {
            _trace.Steps.Add("query>");
            var result = await next();
            _trace.Steps.Add("<query");
            return result;
        }
    }

    // Corta la tubería sin llamar al manejador.
    public sealed class ShortCircuitBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
        where TRequest : notnull
        where TResult : Result
    {
        public Task<TResult> HandleAsync(TRequest request, NextHandler<TResult> next, CancellationToken cancellationToken)
            => Task.FromResult(Result.FailureFor<TResult>(Error.Validation("Tests.Cortado", "Cortado por un comportamiento.")));
    }

    public sealed class ThrowingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
        where TRequest : notnull
        where TResult : Result
    {
        public Task<TResult> HandleAsync(TRequest request, NextHandler<TResult> next, CancellationToken cancellationToken)
            => throw new InvalidOperationException("fallo del comportamiento");
    }

    // Manejador con una dependencia que sólo implementa IAsyncDisposable: el ámbito de la comprobación tiene que cerrarse en asíncrono.
    public sealed class OnlyAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public sealed record UsesAsyncDisposableQuery : IQuery<Result>;

    public sealed class UsesAsyncDisposableQueryHandler : IQueryHandler<UsesAsyncDisposableQuery, Result>
    {
        public UsesAsyncDisposableQueryHandler(OnlyAsyncDisposable dependency) => _ = dependency;

        public Task<Result> HandleAsync(UsesAsyncDisposableQuery query, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    // Comprobación de validación de prueba.
    public sealed class FakeValidationCheck : IValidationStartupCheck
    {
        public static int Calls { get; set; }

        public Result Check(IMediatorCatalog catalog, IServiceProvider services)
        {
            Calls++;
            return Result.Success();
        }
    }
}
