using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Mediator.Extensions;
using Kitpymes.Core.Mediator.Settings;
using Kitpymes.Core.Mediator.Tests.Ok;
using Kitpymes.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Kitpymes.Core.Mediator.Tests
{
    [TestClass]
    public class RegistrationTest
    {
        private static readonly Assembly Ok = typeof(DoCommand).Assembly;

        [TestMethod]
        public void LoadMediator_Passing_Assembly_Returns_HandlersRegisteredAsTransient()
        {
            var services = new ServiceCollection();

            services.LoadMediator(o => o.WithAssemblies(Ok));

            var handler = services.Single(x => x.ServiceType == typeof(ICommandHandler<DoCommand, Result>));
            Assert.AreEqual(ServiceLifetime.Transient, handler.Lifetime);
            Assert.AreEqual(typeof(DoCommandHandler), handler.ImplementationType);
            Assert.AreEqual(ServiceLifetime.Scoped, services.Single(x => x.ServiceType == typeof(ISender)).Lifetime);
            Assert.AreEqual(ServiceLifetime.Scoped, services.Single(x => x.ServiceType == typeof(IPublisher)).Lifetime);
            Assert.AreEqual(3, services.Count(x => x.ServiceType == typeof(INotificationHandler<SomethingHappened>)));
        }

        [TestMethod]
        public void LoadMediator_Passing_SameAssemblyTwice_Returns_NoDuplicates()
        {
            var services = new ServiceCollection();

            services.LoadMediator(o => o.WithAssemblies(Ok, Ok));
            services.LoadMediator(o => o.WithAssemblies(Ok));

            Assert.AreEqual(1, services.Count(x => x.ServiceType == typeof(ICommandHandler<DoCommand, Result>)));
            Assert.AreEqual(1, services.Count(x => x.ServiceType == typeof(ISender)));
            Assert.AreEqual(1, services.Count(x => x.ServiceType == typeof(IMediatorCatalog)));
            Assert.AreEqual(1, services.Count(x => x.ServiceType == typeof(IHostedService)));
        }

        [TestMethod]
        public void LoadMediator_Passing_OpenGenericHandler_Returns_NotRegistered()
        {
            var services = new ServiceCollection();

            services.LoadMediator(o => o.WithAssemblies(Ok));

            Assert.IsFalse(services.Any(x => x.ImplementationType is { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(OpenGenericHandler<>)));
        }

        [TestMethod]
        public void LoadMediator_Passing_AbstractHandlerAndMarkerInterface_Returns_OnlyConcreteTypes()
        {
            var services = new ServiceCollection();

            services.LoadMediator(o => o.WithAssemblies(Ok));
            using var provider = services.BuildServiceProvider();

            var handler = services.Single(x => x.ServiceType == typeof(IQueryHandler<InheritedHandlerQuery, Result>));
            Assert.AreEqual(typeof(ConcreteInheritedHandler), handler.ImplementationType);
            Assert.IsFalse(provider.GetRequiredService<IMediatorCatalog>().Commands.Contains(typeof(IMarkerCommand)));
        }

        [TestMethod]
        public void LoadMediator_Passing_WithoutStartupVerification_Returns_OnlyWarningHostedService()
        {
            var services = new ServiceCollection();

            services.LoadMediator(o => o.WithAssemblies(Ok).WithStartupVerification(false));

            // Sólo el aviso: la comprobación no se registra.
            Assert.AreEqual("MediatorStartupWarning", services.Single(x => x.ServiceType == typeof(IHostedService)).ImplementationType!.Name);
        }

        [TestMethod]
        public void LoadMediator_Passing_NullSettings_Returns_MediatorWithoutAssemblies()
        {
            var services = new ServiceCollection();

            services.LoadMediator((MediatorSettings?)null);

            Assert.IsTrue(services.Any(x => x.ServiceType == typeof(ISender)));
        }

        [TestMethod]
        public async Task LoadMediator_Passing_Settings_Returns_ValidationFlagInCatalog()
        {
            var settings = new MediatorSettings { Validation = false, StartupVerification = false };
            settings.Assemblies.Add(Ok);
            var services = new ServiceCollection();
            services.AddScoped<Trace>();
            services.AddScoped<OnlyAsyncDisposable>();

            services.LoadMediator(settings);
            using var provider = services.BuildServiceProvider();

            Assert.IsTrue((await MediatorVerifier.VerifyAsync(provider)).IsSuccess);
        }

        [TestMethod]
        public void LoadMediator_Passing_NullArguments_Throws_ArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ((IServiceCollection)null!).LoadMediator(o => o.WithAssemblies(Ok)));
            Assert.ThrowsExactly<ArgumentNullException>(() => ((IServiceCollection)null!).LoadMediator((MediatorSettings?)null));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServiceCollection().LoadMediator((Action<MediatorOptions>)null!));
        }

        [TestMethod]
        public void MediatorOptions_Passing_Values_Returns_SettingsWithThemAndNoDuplicates()
        {
            var options = new MediatorOptions()
                .WithAssemblies(Ok, Ok)
                .WithValidation(false)
                .WithStartupVerification(false);

            Assert.AreEqual(1, options.MediatorSettings.Assemblies.Count);
            Assert.IsFalse(options.MediatorSettings.Validation);
            Assert.IsFalse(options.MediatorSettings.StartupVerification);
            Assert.IsTrue(options.WithValidation().MediatorSettings.Validation);
            Assert.IsTrue(options.WithStartupVerification().MediatorSettings.StartupVerification);
        }

        [TestMethod]
        public void MediatorSettings_Passing_Nothing_Returns_ValidationAndStartupVerificationOn()
        {
            var actual = new MediatorSettings();

            Assert.IsTrue(actual.Validation);
            Assert.IsTrue(actual.StartupVerification);
            Assert.AreEqual(0, actual.Assemblies.Count);
        }

        [TestMethod]
        public void WithAssemblies_Passing_Null_Throws_ArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new MediatorOptions().WithAssemblies(null!));
            Assert.ThrowsExactly<ArgumentNullException>(() => new MediatorOptions().WithAssemblies(Ok, null!));
        }

        [TestMethod]
        public void Catalog_Passing_Assembly_Returns_CommandsQueriesAndNotifications()
        {
            var services = new ServiceCollection();
            services.LoadMediator(o => o.WithAssemblies(Ok));
            using var provider = services.BuildServiceProvider();

            var catalog = provider.GetRequiredService<IMediatorCatalog>();

            CollectionAssert.AreEquivalent(new[] { typeof(DoCommand), typeof(PingCommand) }, catalog.Commands.ToArray());
            CollectionAssert.AreEquivalent(new[] { typeof(GetNumberQuery), typeof(GetTextQuery), typeof(UsesAsyncDisposableQuery), typeof(InheritedHandlerQuery) }, catalog.Queries.ToArray());
            CollectionAssert.AreEquivalent(
                new[] { typeof(SomethingHappened), typeof(SomethingDerivedHappened), typeof(NobodyListens), typeof(Pinged) },
                catalog.Notifications.ToArray());
        }
    }
}
