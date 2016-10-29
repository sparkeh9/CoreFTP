namespace CoreFtp.Tests.Integration.Logger
{
    using System;
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public static class XUnitConsoleLoggerExtensions
    {
        public static ILoggerFactory AddXunitConsole( this ILoggerFactory factory, ITestOutputHelper outputHelper, Func<string, LogLevel, bool> filter = null )
        {
            if ( outputHelper != null )
                factory.AddProvider( new XUnitConsoleLoggerProvider( filter, outputHelper ) );

            return factory;
        }

        public static ILoggerFactory AddXunitConsole( this ILoggerFactory factory, ITestOutputHelper outputHelper, LogLevel minLevel )
        {
            return AddXunitConsole( factory, outputHelper, ( _, logLevel ) => logLevel >= minLevel );
        }
    }
}