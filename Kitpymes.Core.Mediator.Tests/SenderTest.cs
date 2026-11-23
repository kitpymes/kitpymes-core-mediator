using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Mediator.Tests.Ok;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kitpymes.Core.Mediator.Tests
{
    [TestClass]
    public class SenderTest
    {
        [TestMethod]
        public async Task SendAsync_Passing_Command_Returns_HandlerResult()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Ok));

            Assert.IsTrue(actual.IsSuccess);
            CollectionAssert.AreEqual(new[] { "handler" }, scope.ServiceProvider.GetRequiredService<Trace>().Steps);
        }

        [TestMethod]
        public async Task SendAsync_Passing_CommandThatFails_Returns_SameError()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Fail));

            Assert.IsTrue(actual.IsFailure);
            Assert.AreEqual("Tests.Conflicto", actual.Error!.Code);
        }

        [TestMethod]
        public async Task SendAsync_Passing_QueryWithValue_Returns_TypedValue()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new GetNumberQuery(21));

            Assert.IsTrue(actual.IsSuccess);
            Assert.AreEqual(42, actual.Value);
        }

        [TestMethod]
        public async Task SendAsync_Passing_QueryThatFails_Returns_TypedFailure()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new GetNumberQuery(-1));

            Assert.IsTrue(actual.IsFailure);
            Assert.AreEqual("Tests.Negativo", actual.Error!.Code);
        }

        [TestMethod]
        public async Task SendAsync_Passing_SameQueryTypeTwice_Returns_IndependentResults()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var first = await sender.SendAsync(new GetNumberQuery(1));
            var second = await sender.SendAsync(new GetNumberQuery(2));
            var text = await sender.SendAsync(new GetTextQuery());

            Assert.AreEqual(2, first.Value);
            Assert.AreEqual(4, second.Value);
            Assert.AreEqual("texto", text.Value);
        }

        [TestMethod]
        public async Task SendAsync_Passing_CommandOfClassWithSeveralHandlers_Returns_ItsResult()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new PingCommand());

            Assert.AreEqual("pong", actual.Value);
        }

        [TestMethod]
        [DataRow(Behavior.Throw)]
        [DataRow(Behavior.ThrowBeforeTask)]
        [DataRow(Behavior.NullTask)]
        [DataRow(Behavior.NullResult)]
        public async Task SendAsync_Passing_HandlerThatBreaks_Returns_UnexpectedError(Behavior behavior)
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(behavior));

            Assert.IsTrue(actual.IsFailure);
            Assert.AreEqual(MediatorErrors.Unexpected, actual.Error!.Code);
            Assert.AreEqual(ErrorType.Unexpected, actual.Error.Type);
            Assert.AreEqual(MediatorErrors.UnexpectedMessage, actual.Error.Message);
        }

        [TestMethod]
        [DataRow(Behavior.NullTask, "tarea nula")]
        [DataRow(Behavior.NullResult, "resultado nulo")]
        public async Task SendAsync_Passing_HandlerThatReturnsNull_Returns_LogWithRequestType(Behavior behavior, string expected)
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            _ = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(behavior));

            var entry = provider.GetRequiredService<TestLog>().Entries.Single(x => x.Level == LogLevel.Error);
            StringAssert.Contains(entry.Exception!.Message, typeof(DoCommand).FullName + " devolvió");
            StringAssert.Contains(entry.Exception.Message, expected);
        }

        [TestMethod]
        public async Task SendAsync_Passing_HandlerThatThrows_Returns_LogWithTypeAndExceptionButNotContent()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            _ = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Throw, "contraseña-secreta"));

            var entry = provider.GetRequiredService<TestLog>().Entries.Single(x => x.Level == LogLevel.Error);
            StringAssert.Contains(entry.Message, typeof(DoCommand).FullName);
            Assert.AreEqual("fallo del manejador", entry.Exception!.Message);
            Assert.IsFalse(entry.Message.Contains("contraseña-secreta", StringComparison.Ordinal));
            Assert.IsFalse(entry.Exception.ToString().Contains("contraseña-secreta", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task SendAsync_Passing_CancelledToken_Throws_OperationCanceledException()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Cancel), cancellation.Token));

            Assert.AreEqual(0, provider.GetRequiredService<TestLog>().Entries.Count(x => x.Level == LogLevel.Error));
        }

        [TestMethod]
        public async Task SendAsync_Passing_LinkedCancelledToken_Throws_OperationCanceledException()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();
            using var outer = new CancellationTokenSource();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(outer.Token);
            outer.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Cancel), linked.Token));
        }

        [TestMethod]
        public async Task SendAsync_Passing_CancellationNotRequestedByCaller_Returns_UnexpectedError()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Cancel));

            Assert.AreEqual(MediatorErrors.Unexpected, actual.Error!.Code);
            Assert.AreEqual(1, provider.GetRequiredService<TestLog>().Entries.Count(x => x.Level == LogLevel.Error));
        }

        [TestMethod]
        public async Task SendAsync_Passing_CommandWithoutHandlerRegistered_Returns_UnexpectedError()
        {
            using var provider = TestHost.Build(services => services.RemoveAll<ICommandHandler<DoCommand, Result>>());
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Ok));

            Assert.AreEqual(MediatorErrors.Unexpected, actual.Error!.Code);
        }

        [TestMethod]
        public async Task SendAsync_Passing_NullCommand_Throws_ArgumentNullException()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => sender.SendAsync((ICommand<Result>)null!));
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => sender.SendAsync((IQuery<Result>)null!));
        }

        [TestMethod]
        public async Task SendAsync_Passing_LoggerThatThrows_Returns_UnexpectedError()
        {
            using var provider = TestHost.Build(services => services.AddSingleton<ILoggerProvider, ThrowingLoggerProvider>());
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Throw));

            Assert.AreEqual(MediatorErrors.Unexpected, actual.Error!.Code);
        }

        [TestMethod]
        public async Task SendAsync_Passing_NoLoggerRegistered_Returns_UnexpectedError()
        {
            var services = new ServiceCollection();
            services.AddScoped<Trace>();
            Kitpymes.Core.Mediator.Extensions.MediatorServiceCollectionExtensions.LoadMediator(services, o => o.WithAssemblies(typeof(DoCommand).Assembly).WithValidation(false));
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Throw));

            Assert.AreEqual(MediatorErrors.Unexpected, actual.Error!.Code);
        }

        [TestMethod]
        public void Sender_Passing_ResolvedFromRootWithScopeValidation_Throws_InvalidOperationException()
        {
            using var provider = TestHost.Build();

            Assert.ThrowsExactly<InvalidOperationException>(() => provider.GetRequiredService<ISender>());
        }

        [TestMethod]
        public void SenderAndPublisher_Passing_SameScope_Returns_SameInstance()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            Assert.AreSame<object>(scope.ServiceProvider.GetRequiredService<ISender>(), scope.ServiceProvider.GetRequiredService<IPublisher>());
            Assert.AreSame(scope.ServiceProvider.GetRequiredService<ISender>(), scope.ServiceProvider.GetRequiredService<ISender>());
        }

        private sealed class ThrowingLoggerProvider : ILoggerProvider
        {
            public ILogger CreateLogger(string categoryName) => new ThrowingLogger();

            public void Dispose()
            {
            }

            private sealed class ThrowingLogger : ILogger
            {
                public IDisposable? BeginScope<TState>(TState state)
                    where TState : notnull
                    => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                    => throw new InvalidOperationException("el log falla");
            }
        }
    }
}
