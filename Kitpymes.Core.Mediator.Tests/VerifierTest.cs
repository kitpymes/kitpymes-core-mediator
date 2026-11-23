using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Mediator.Extensions;
using Kitpymes.Core.Mediator.Settings;
using Kitpymes.Core.Mediator.Tests.Ok;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mal = Kitpymes.Core.Mediator.Tests.Mal;

namespace Kitpymes.Core.Mediator.Tests
{
    [TestClass]
    public class VerifierTest
    {
        private static async Task<Result> VerifyMal(Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();
            services.LoadMediator(o => o.WithAssemblies(typeof(Mal.OrphanCommand).Assembly).WithValidation(false));
            configure?.Invoke(services);

            await using var provider = services.BuildServiceProvider();
            return await MediatorVerifier.VerifyAsync(provider);
        }

        [TestMethod]
        public async Task Verify_Passing_CorrectConfiguration_Returns_Success()
        {
            using var provider = TestHost.Build();

            var actual = await MediatorVerifier.VerifyAsync(provider);

            Assert.IsTrue(actual.IsSuccess, actual.Error?.Message);
        }

        [TestMethod]
        public async Task Verify_Passing_Null_Throws_ArgumentNullException()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => MediatorVerifier.VerifyAsync(null!));
        }

        [TestMethod]
        public async Task Verify_Passing_ProviderWithoutMediator_Returns_FailureLoadMediatorMissing()
        {
            using var provider = new ServiceCollection().BuildServiceProvider();

            var actual = await MediatorVerifier.VerifyAsync(provider);

            Assert.AreEqual(MediatorErrors.Configuration, actual.Error!.Code);
            StringAssert.Contains(actual.Error.Message, "LoadMediator");
        }

        [TestMethod]
        public async Task Verify_Passing_NoAssemblies_Returns_Failure()
        {
            var services = new ServiceCollection();
            services.LoadMediator(o => o.WithValidation(false));
            using var provider = services.BuildServiceProvider();

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "WithAssemblies");
        }

        [TestMethod]
        [DataRow("OrphanCommand (Result) no tiene manejador.")]
        [DataRow("TwiceHandledQuery (Result) tiene 2 manejadores")]
        [DataRow("BothCommandAndQuery implementa a la vez")]
        [DataRow("TwoResultsCommand implementa a la vez")]
        [DataRow("No se pueden crear el manejador de NeedsDependencyCommand (Result)")]
        [DataRow("No se pueden crear los manejadores de BrokenNotification")]
        public async Task Verify_Passing_WrongConfiguration_Returns_FailureWithEachProblem(string expected)
        {
            var actual = await VerifyMal();

            Assert.AreEqual(MediatorErrors.Configuration, actual.Error!.Code);
            StringAssert.Contains(actual.Error.Message, expected);
        }

        [TestMethod]
        public async Task Verify_Passing_WrongConfiguration_Returns_ProblemsInAlphabeticalOrderAndGenericNames()
        {
            var message = (await VerifyMal()).Error!.Message;

            StringAssert.Contains(message, "ICommand<Result> y ICommand<Result<Int32>>");
            Assert.IsTrue(message.IndexOf("BothCommandAndQuery implementa", StringComparison.Ordinal) < message.IndexOf("TwoResultsCommand implementa", StringComparison.Ordinal));
            Assert.IsTrue(message.IndexOf("NeedsDependencyCommand", StringComparison.Ordinal) < message.IndexOf("OrphanCommand (Result)", StringComparison.Ordinal));
            Assert.IsTrue(message.IndexOf("OrphanCommand (Result)", StringComparison.Ordinal) < message.IndexOf("TwoResultsCommand (Result)", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task Verify_Passing_RequestFromUnscannedAssembly_Returns_ItInCatalogWithItsHandler()
        {
            var services = new ServiceCollection();
            services.LoadMediator(o => o.WithAssemblies(typeof(Mal.OrphanCommand).Assembly).WithValidation(false));
            using var provider = services.BuildServiceProvider();

            var catalog = provider.GetRequiredService<IMediatorCatalog>();

            Assert.IsTrue(catalog.Commands.Contains(typeof(Kitpymes.Core.Mediator.Tests.Contratos.ContractCommand)));
            Assert.IsFalse((await MediatorVerifier.VerifyAsync(provider)).Error!.Message.Contains("ContractCommand", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task Verify_Passing_CommandBehaviorWithMissingDependency_Returns_Failure()
        {
            using var provider = TestHost.Build(services => services.AddTransient(typeof(ICommandBehavior<,>), typeof(NeedsMissingCommandBehavior<,>)));

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "No se pueden crear los comportamientos de DoCommand (Result)");
        }

        [TestMethod]
        public async Task Verify_Passing_ValidationCheckRegisteredOnlyAsValidationCheck_Returns_SuccessRunningItOnce()
        {
            FakeValidationCheck.Calls = 0;
            using var provider = TestHost.Build(services => services.AddSingleton<IValidationStartupCheck, FakeValidationCheck>(), o => o.WithValidation());

            var actual = await MediatorVerifier.VerifyAsync(provider);

            Assert.IsTrue(actual.IsSuccess, actual.Error?.Message);
            Assert.AreEqual(1, FakeValidationCheck.Calls);
        }

        [TestMethod]
        public async Task Verify_Passing_SecondHandlerAddedByHand_Returns_FailureTwoHandlers()
        {
            using var provider = TestHost.Build(services => services.AddTransient<ICommandHandler<DoCommand, Result>, DoCommandHandler>());

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "DoCommand (Result) tiene 2 manejadores");
        }

        [TestMethod]
        public async Task Verify_Passing_BehaviorWithMissingDependency_Returns_Failure()
        {
            using var provider = TestHost.Build(services => services.AddTransient(typeof(IPipelineBehavior<,>), typeof(NeedsMissingBehavior<,>)));

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "No se pueden crear los comportamientos de");
        }

        [TestMethod]
        public async Task Verify_Passing_ValidationRequiredWithoutCheck_Returns_Failure()
        {
            using var provider = TestHost.Build(options: o => o.WithValidation());

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "WithValidation(false)");
        }

        [TestMethod]
        public async Task Verify_Passing_ValidationRequiredWithCheckRegisteredTwice_Returns_SuccessRunningItOnce()
        {
            FakeValidationCheck.Calls = 0;
            using var provider = TestHost.Build(
                services =>
                {
                    services.AddSingleton<IStartupCheck, FakeValidationCheck>();
                    services.AddSingleton<IValidationStartupCheck, FakeValidationCheck>();
                },
                o => o.WithValidation());

            var actual = await MediatorVerifier.VerifyAsync(provider);

            Assert.IsTrue(actual.IsSuccess, actual.Error?.Message);
            Assert.AreEqual(1, FakeValidationCheck.Calls);
        }

        [TestMethod]
        public async Task Verify_Passing_CheckThatFails_Returns_FailureWithItsMessage()
        {
            using var provider = TestHost.Build(services => services.AddSingleton<IStartupCheck>(new LambdaCheck(() => Result.Failure(Error.Conflict("Tests.Check", "La comprobación falla.")))));

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "La comprobación falla.");
        }

        [TestMethod]
        public async Task Verify_Passing_CheckThatThrows_Returns_FailureWithException()
        {
            using var provider = TestHost.Build(services => services.AddSingleton<IStartupCheck>(new LambdaCheck(() => throw new InvalidOperationException("la comprobación lanza"))));

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "la comprobación lanza");
        }

        [TestMethod]
        public async Task Verify_Passing_CheckThatReturnsNullTask_Returns_Failure()
        {
            using var provider = TestHost.Build(services => services.AddSingleton<IStartupCheck>(new NullTaskCheck()));

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "resultado nulo");
        }

        [TestMethod]
        public async Task Verify_Passing_LibraryDisablesValidationAfterApplication_Returns_ValidationStillRequired()
        {
            using var provider = TestHost.Build(
                services => services.LoadMediator(o => o.WithAssemblies(typeof(DoCommand).Assembly).WithValidation(false)),
                o => o.WithValidation());

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "Falta la comprobación de validación");
        }

        [TestMethod]
        public async Task Verify_Passing_CheckThatReturnsNull_Returns_Failure()
        {
            using var provider = TestHost.Build(services => services.AddSingleton<IStartupCheck>(new LambdaCheck(() => null!)));

            var actual = await MediatorVerifier.VerifyAsync(provider);

            StringAssert.Contains(actual.Error!.Message, "resultado nulo");
        }

        [TestMethod]
        public async Task Verify_Passing_CheckThatSucceeds_Returns_SuccessAndReceivesCatalog()
        {
            IMediatorCatalog? seen = null;
            using var provider = TestHost.Build(services => services.AddSingleton<IStartupCheck>(new LambdaCheck((catalog, _) =>
            {
                seen = catalog;
                return Result.Success();
            })));

            var actual = await MediatorVerifier.VerifyAsync(provider);

            Assert.IsTrue(actual.IsSuccess, actual.Error?.Message);
            Assert.IsTrue(seen!.Commands.Contains(typeof(DoCommand)));
            Assert.IsTrue(seen.Queries.Contains(typeof(GetNumberQuery)));
            Assert.IsTrue(seen.Notifications.Contains(typeof(SomethingHappened)));
        }

        [TestMethod]
        public async Task Verify_Passing_HandlerWithAsyncDisposableDependency_Returns_Success()
        {
            // TestHost registra OnlyAsyncDisposable, que necesita el manejador de UsesAsyncDisposableQuery.
            using var provider = TestHost.Build();

            var actual = await MediatorVerifier.VerifyAsync(provider);

            Assert.IsTrue(actual.IsSuccess, actual.Error?.Message);
        }

        [TestMethod]
        public async Task Verify_Passing_ManyProblems_Returns_OneLinePerProblem()
        {
            var actual = await VerifyMal();

            var lines = actual.Error!.Message.Split(Environment.NewLine);
            Assert.AreEqual("El mediador no está bien configurado:", lines[0]);
            Assert.IsTrue(lines.Skip(1).All(x => x.StartsWith("- ", StringComparison.Ordinal)));
            Assert.IsTrue(lines.Length >= 7);
        }

        [TestMethod]
        public async Task StartAsync_Passing_WrongConfiguration_Throws_InvalidOperationExceptionAndHostDoesNotStart()
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.LoadMediator(o => o.WithAssemblies(typeof(Mal.OrphanCommand).Assembly).WithValidation(false));
            using var host = builder.Build();

            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => host.StartAsync());

            StringAssert.Contains(exception.Message, "OrphanCommand");
        }

        [TestMethod]
        public async Task StartAsync_Passing_CorrectConfiguration_Returns_HostStarted()
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddScoped<Trace>();
            builder.Services.AddScoped<OnlyAsyncDisposable>();
            builder.Services.LoadMediator(o => o.WithAssemblies(typeof(DoCommand).Assembly).WithValidation(false));
            using var host = builder.Build();

            await host.StartAsync();
            await host.StopAsync();
        }

        [TestMethod]
        public async Task StartAsync_Passing_WrongConfigurationWithoutStartupVerification_Returns_HostStarted()
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.LoadMediator(o => o.WithAssemblies(typeof(Mal.OrphanCommand).Assembly).WithValidation(false).WithStartupVerification(false));
            using var host = builder.Build();

            await host.StartAsync();
            await host.StopAsync();

            Assert.IsFalse((await MediatorVerifier.VerifyAsync(host.Services)).IsSuccess);
        }

        [TestMethod]
        public async Task StartAsync_Passing_WithoutStartupVerification_Returns_WarningInLog()
        {
            var log = new TestLog();
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders().AddProvider(log);
            builder.Services.LoadMediator(o => o.WithAssemblies(typeof(DoCommand).Assembly).WithValidation(false).WithStartupVerification(false));
            using var host = builder.Build();

            await host.StartAsync();
            await host.StopAsync();

            var warning = log.Entries.Single(x => x.Level == LogLevel.Warning && x.Message.Contains("WithStartupVerification(false)", StringComparison.Ordinal));
            StringAssert.Contains(warning.Message, "tests de integración");
        }

        [TestMethod]
        public async Task StartAsync_Passing_VerificationDisabledInOneCallAndEnabledInAnother_Returns_VerifiedWithoutWarning()
        {
            var log = new TestLog();
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders().AddProvider(log);
            builder.Services.LoadMediator(o => o.WithAssemblies(typeof(Mal.OrphanCommand).Assembly).WithValidation(false).WithStartupVerification(false));
            builder.Services.LoadMediator(o => o.WithValidation(false));
            using var host = builder.Build();

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => host.StartAsync());

            Assert.IsFalse(log.Entries.Any(x => x.Level == LogLevel.Warning && x.Message.Contains("WithStartupVerification(false)", StringComparison.Ordinal)));
        }

        private sealed class NeedsMissingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
            where TRequest : notnull
            where TResult : Result
        {
            public NeedsMissingBehavior(Mal.IMissingDependency dependency) => _ = dependency;

            public Task<TResult> HandleAsync(TRequest request, NextHandler<TResult> next, CancellationToken cancellationToken) => next();
        }

        private sealed class NeedsMissingCommandBehavior<TCommand, TResult> : ICommandBehavior<TCommand, TResult>
            where TCommand : ICommand<TResult>
            where TResult : Result
        {
            public NeedsMissingCommandBehavior(Mal.IMissingDependency dependency) => _ = dependency;

            public Task<TResult> HandleAsync(TCommand command, NextHandler<TResult> next, CancellationToken cancellationToken) => next();
        }

        private sealed class NullTaskCheck : IStartupCheck
        {
            public Task<Result> CheckAsync(IMediatorCatalog catalog, IServiceProvider services, CancellationToken cancellationToken) => null!;
        }

        private sealed class LambdaCheck : IStartupCheck
        {
            private readonly Func<IMediatorCatalog, IServiceProvider, Result> _check;

            public LambdaCheck(Func<Result> check) => _check = (_, _) => check();

            public LambdaCheck(Func<IMediatorCatalog, IServiceProvider, Result> check) => _check = check;

            public Task<Result> CheckAsync(IMediatorCatalog catalog, IServiceProvider services, CancellationToken cancellationToken)
                => Task.FromResult(_check(catalog, services));
        }
    }
}
