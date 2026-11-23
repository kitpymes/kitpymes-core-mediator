// Tipos de prueba mal configurados a propósito: los escanean sólo los tests de MediatorVerifier.
using System;
using System.Threading;
using System.Threading.Tasks;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Results;

namespace Kitpymes.Core.Mediator.Tests.Mal
{
    // Comando sin manejador.
    public sealed record OrphanCommand : ICommand<Result>;

    // Comando y consulta a la vez.
    public sealed record BothCommandAndQuery : ICommand<Result>, IQuery<Result>;

    public sealed class BothHandler : ICommandHandler<BothCommandAndQuery, Result>, IQueryHandler<BothCommandAndQuery, Result>
    {
        public Task<Result> HandleAsync(BothCommandAndQuery request, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    // Comando con dos resultados.
    public sealed record TwoResultsCommand : ICommand<Result>, ICommand<Result<int>>;

    // Consulta con dos manejadores.
    public sealed record TwiceHandledQuery : IQuery<Result>;

    public sealed class TwiceHandledQueryHandlerOne : IQueryHandler<TwiceHandledQuery, Result>
    {
        public Task<Result> HandleAsync(TwiceHandledQuery query, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    public sealed class TwiceHandledQueryHandlerTwo : IQueryHandler<TwiceHandledQuery, Result>
    {
        public Task<Result> HandleAsync(TwiceHandledQuery query, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    // Manejador con una dependencia que nadie registra.
    public interface IMissingDependency
    {
    }

    public sealed record NeedsDependencyCommand : ICommand<Result>;

    public sealed class NeedsDependencyCommandHandler : ICommandHandler<NeedsDependencyCommand, Result>
    {
        public NeedsDependencyCommandHandler(IMissingDependency dependency) => _ = dependency;

        public Task<Result> HandleAsync(NeedsDependencyCommand command, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    // Notificación cuyo manejador necesita una dependencia que nadie registra.
    public sealed record BrokenNotification : INotification;

    public sealed class BrokenNotificationHandler : INotificationHandler<BrokenNotification>
    {
        public BrokenNotificationHandler(IMissingDependency dependency) => _ = dependency;

        public Task<Result> HandleAsync(BrokenNotification notification, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }
}
