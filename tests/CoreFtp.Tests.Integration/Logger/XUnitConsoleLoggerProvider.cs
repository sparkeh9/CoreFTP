namespace CoreFtp.Tests.Integration.Logger
{
    using System;
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public class XUnitConsoleLoggerProvider : ILoggerProvider
    {
        private readonly Func<string, LogLevel, bool> filter;
        private readonly ITestOutputHelper OutputHelper;

        public XUnitConsoleLoggerProvider( Func<string, LogLevel, bool> filter, ITestOutputHelper outputHelper )
        {
            this.filter = filter;
            OutputHelper = outputHelper;
        }

        public ILogger CreateLogger( string categoryName )
        {
            return new XUnitConsoleLogger( categoryName, filter, OutputHelper );
        }

        public void Dispose() {}
    }
}