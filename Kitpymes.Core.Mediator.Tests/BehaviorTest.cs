using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Mediator.Tests.Ok;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Kitpymes.Core.Mediator.Tests
{
    [TestClass]
    public class BehaviorTest
    {
        private static ServiceProvider BuildWithAllBehaviors()
            => TestHost.Build(services =>
            {
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(GeneralBehavior<,>));
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SecondGeneralBehavior<,>));
                services.AddTransient(typeof(ICommandBehavior<,>), typeof(CommandOnlyBehavior<,>));
                services.AddTransient(typeof(IQueryBehavior<,>), typeof(QueryOnlyBehavior<,>));
            });

        [TestMethod]
        public async Task SendAsync_Passing_CommandWithBehaviors_Returns_GeneralOutsideSpecificInsideInRegistrationOrder()
        {
            using var provider = BuildWithAllBehaviors();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Ok));

            Assert.IsTrue(actual.IsSuccess);
            CollectionAssert.AreEqual(
                new[] { "general>", "general2>", "command>", "handler", "<command", "<general2", "<general" },
                scope.ServiceProvider.GetRequiredService<Trace>().Steps);
        }

        [TestMethod]
        public async Task SendAsync_Passing_QueryWithBehaviors_Returns_QueryBehaviorAndNotCommandBehavior()
        {
            using var provider = BuildWithAllBehaviors();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new GetNumberQuery(1));

            Assert.AreEqual(2, actual.Value);
            CollectionAssert.AreEqual(
                new[] { "general>", "general2>", "query>", "handler", "<query", "<general2", "<general" },
                scope.ServiceProvider.GetRequiredService<Trace>().Steps);
        }

        [TestMethod]
        public async Task SendAsync_Passing_BehaviorThatShortCircuits_Returns_ItsResultWithoutCallingHandler()
        {
            using var provider = TestHost.Build(services => services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ShortCircuitBehavior<,>)));
            using var scope = provider.CreateScope();

            var command = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Ok));
            var query = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new GetNumberQuery(1));

            Assert.AreEqual("Tests.Cortado", command.Error!.Code);
            Assert.AreEqual("Tests.Cortado", query.Error!.Code);
            Assert.AreEqual(0, scope.ServiceProvider.GetRequiredService<Trace>().Steps.Count);
        }

        [TestMethod]
        public async Task SendAsync_Passing_BehaviorThatThrows_Returns_UnexpectedError()
        {
            using var provider = TestHost.Build(services => services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ThrowingBehavior<,>)));
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new GetNumberQuery(1));

            Assert.AreEqual(MediatorErrors.Unexpected, actual.Error!.Code);
            Assert.IsTrue(actual.IsFailure);
        }

        [TestMethod]
        public async Task SendAsync_Passing_HandlerThatThrowsInsideBehaviors_Returns_UnexpectedErrorThroughBehaviors()
        {
            using var provider = BuildWithAllBehaviors();
            using var scope = provider.CreateScope();

            var actual = await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new DoCommand(Behavior.Throw));

            Assert.AreEqual(MediatorErrors.Unexpected, actual.Error!.Code);
        }
    }
}
