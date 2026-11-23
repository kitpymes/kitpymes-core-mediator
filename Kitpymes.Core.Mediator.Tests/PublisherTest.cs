using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Mediator.Tests.Ok;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kitpymes.Core.Mediator.Tests
{
    [TestClass]
    public class PublisherTest
    {
        [TestMethod]
        public async Task PublishAsync_Passing_Notification_Returns_SuccessAfterAllHandlersInAlphabeticalOrder()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new SomethingHappened("ok"));

            Assert.IsTrue(actual.IsSuccess);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, scope.ServiceProvider.GetRequiredService<Trace>().Steps);
        }

        [TestMethod]
        public async Task PublishAsync_Passing_HandlerThatFails_Returns_ItsErrorAndStops()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new SomethingHappened("falla-en-A"));

            Assert.AreEqual("Tests.FallaA", actual.Error!.Code);
            CollectionAssert.AreEqual(new[] { "A" }, scope.ServiceProvider.GetRequiredService<Trace>().Steps);
        }

        [TestMethod]
        public async Task PublishAsync_Passing_HandlerThatThrows_Returns_UnexpectedErrorStopsAndLogsHandlerType()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new SomethingHappened("lanza-en-B"));

            Assert.AreEqual(MediatorErrors.Unexpected, actual.Error!.Code);
            CollectionAssert.AreEqual(new[] { "A", "B" }, scope.ServiceProvider.GetRequiredService<Trace>().Steps);

            var entry = provider.GetRequiredService<TestLog>().Entries.Single(x => x.Level == LogLevel.Error);
            StringAssert.Contains(entry.Message, typeof(SomethingHappened).FullName);
            StringAssert.Contains(entry.Message, typeof(BSecondHandler).FullName);
            Assert.IsFalse(entry.Message.Contains("lanza-en-B", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task PublishAsync_Passing_HandlerThatReturnsNull_Returns_UnexpectedError()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new SomethingHappened("nulo-en-C"));

            Assert.AreEqual(MediatorErrors.Unexpected, actual.Error!.Code);
        }

        [TestMethod]
        public async Task PublishAsync_Passing_NotificationWithoutHandlers_Returns_Success()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new NobodyListens());

            Assert.IsTrue(actual.IsSuccess);
        }

        [TestMethod]
        public async Task PublishAsync_Passing_DerivedNotification_Returns_SuccessWithoutBaseHandlers()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new SomethingDerivedHappened("ok"));

            Assert.IsTrue(actual.IsSuccess);
            Assert.AreEqual(0, scope.ServiceProvider.GetRequiredService<Trace>().Steps.Count);
        }

        [TestMethod]
        public async Task PublishAsync_Passing_NotificationHandledByMultiHandler_Returns_Success()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new Pinged());

            Assert.IsTrue(actual.IsSuccess);
        }

        [TestMethod]
        public async Task PublishAsync_Passing_CancelledTokenAndHandlerThatHonoursIt_Throws_OperationCanceledException()
        {
            using var provider = TestHost.Build(services => services.AddTransient<INotificationHandler<NobodyListens>, CancellingHandler>());
            using var scope = provider.CreateScope();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new NobodyListens(), cancellation.Token));
        }

        [TestMethod]
        public async Task PublishAsync_Passing_Null_Throws_ArgumentNullException()
        {
            using var provider = TestHost.Build();
            using var scope = provider.CreateScope();

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(null!));
        }

        private sealed class CancellingHandler : INotificationHandler<NobodyListens>
        {
            public Task<Kitpymes.Core.Results.Result> HandleAsync(NobodyListens notification, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Kitpymes.Core.Results.Result.Success());
            }
        }
    }
}
