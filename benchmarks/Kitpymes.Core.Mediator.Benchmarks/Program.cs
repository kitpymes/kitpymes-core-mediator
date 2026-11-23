// Comparación del coste de enviar un comando y publicar una notificación: Kitpymes.Core.Mediator frente a MediatR 12.
// Se ejecuta en la pipeline (job benchmark) en cada rama; es informativo.
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Mediator.Extensions;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;

BenchmarkRunner.Run<MediatorBenchmarks>(args: args);

[MemoryDiagnoser]
public class MediatorBenchmarks
{
    private IServiceScope _kitpymesScope = null!;
    private IServiceScope _mediatrScope = null!;
    private ISender _sender = null!;
    private IPublisher _publisher = null!;
    private MediatR.IMediator _mediatr = null!;

    [Params(0, 2)]
    public int Behaviors { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var kitpymes = new ServiceCollection();
        kitpymes.LoadMediator(o => o.WithAssemblies(typeof(MediatorBenchmarks).Assembly).WithValidation(false).WithStartupVerification(false));
        var mediatr = new ServiceCollection();
        mediatr.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));

        for (var i = 0; i < Behaviors; i++)
        {
            kitpymes.AddTransient(typeof(IPipelineBehavior<,>), typeof(PassThrough<,>));
            mediatr.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(PassThroughMediatR<,>));
        }

        _kitpymesScope = kitpymes.BuildServiceProvider().CreateScope();
        _mediatrScope = mediatr.BuildServiceProvider().CreateScope();
        _sender = _kitpymesScope.ServiceProvider.GetRequiredService<ISender>();
        _publisher = _kitpymesScope.ServiceProvider.GetRequiredService<IPublisher>();
        _mediatr = _mediatrScope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
    }

    [Benchmark(Baseline = true)]
    public Task<Result> MediatR_Send() => _mediatr.Send(new MediatRPing());

    [Benchmark]
    public Task<Result> Kitpymes_Send() => _sender.SendAsync(new Ping());

    [Benchmark]
    public Task MediatR_Publish() => _mediatr.Publish(new MediatRPinged());

    [Benchmark]
    public Task<Result> Kitpymes_Publish() => _publisher.PublishAsync(new Pinged());
}

public sealed record Ping : ICommand<Result>;

public sealed class PingHandler : ICommandHandler<Ping, Result>
{
    public Task<Result> HandleAsync(Ping command, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

public sealed record Pinged : INotification;

public sealed class PingedHandler : INotificationHandler<Pinged>
{
    public Task<Result> HandleAsync(Pinged notification, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

public sealed class PassThrough<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
    where TResult : Result
{
    public Task<TResult> HandleAsync(TRequest request, NextHandler<TResult> next, CancellationToken cancellationToken) => next();
}

public sealed record MediatRPing : MediatR.IRequest<Result>;

public sealed class MediatRPingHandler : MediatR.IRequestHandler<MediatRPing, Result>
{
    public Task<Result> Handle(MediatRPing request, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

public sealed record MediatRPinged : MediatR.INotification;

public sealed class MediatRPingedHandler : MediatR.INotificationHandler<MediatRPinged>
{
    public Task Handle(MediatRPinged notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PassThroughMediatR<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, MediatR.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
}
