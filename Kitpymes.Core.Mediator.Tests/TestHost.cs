using Kitpymes.Core.Mediator.Extensions;
using Kitpymes.Core.Mediator.Settings;
using Kitpymes.Core.Mediator.Tests.Ok;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Kitpymes.Core.Mediator.Tests
{
    /// <summary>Contenedor de pruebas con el mediador sobre los tipos correctos y un log que se puede leer.</summary>
    internal static class TestHost
    {
        public static ServiceProvider Build(Action<IServiceCollection>? configure = null, Action<MediatorOptions>? options = null)
        {
            var services = new ServiceCollection();
            var log = new TestLog();

            services.AddSingleton(log);
            services.AddLogging(x => x.ClearProviders().AddProvider(log).SetMinimumLevel(LogLevel.Trace));
            services.AddScoped<Trace>();
            services.AddScoped<OnlyAsyncDisposable>();
            services.LoadMediator(o =>
            {
                o.WithAssemblies(typeof(DoCommand).Assembly).WithValidation(false);
                options?.Invoke(o);
            });

            configure?.Invoke(services);

            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
        }
    }

    internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    /// <summary>Log en memoria para comprobar qué se registra.</summary>
    internal sealed class TestLog : ILoggerProvider
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new ();

        public ILogger CreateLogger(string categoryName) => new Logger(this);

        public void Dispose()
        {
        }

        private sealed class Logger : ILogger
        {
            private readonly TestLog _log;

            public Logger(TestLog log) => _log = log;

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => _log.Entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }
}
