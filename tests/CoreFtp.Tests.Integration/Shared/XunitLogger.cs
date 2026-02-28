namespace CoreFtp.Tests.Integration.Shared
{
    using System;
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;

        public XunitLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName) => new XunitLogger(_output);

        public void Dispose()
        {
        }
    }

    public class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;

        public XunitLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            _output.WriteLine(formatter(state, exception));
        }
    }
}
