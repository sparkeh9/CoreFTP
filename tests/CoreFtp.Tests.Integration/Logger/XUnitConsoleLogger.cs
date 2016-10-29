namespace CoreFtp.Tests.Integration.Logger
{
    using System;
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public class XUnitConsoleLogger : ILogger
    {
        private readonly string categoryName;
        private readonly Func<string, LogLevel, bool> filter;
        private readonly ITestOutputHelper outputHelper;

        public XUnitConsoleLogger( string categoryName, Func<string, LogLevel, bool> filter, ITestOutputHelper outputHelper )
        {
            this.categoryName = categoryName;
            this.filter = filter;
            this.outputHelper = outputHelper;
        }

        public IDisposable BeginScope< TState >( TState state )
        {
            return null;
        }

        public bool IsEnabled( LogLevel logLevel )
        {
            return filter == null || filter( categoryName, logLevel );
        }

        public void Log< TState >( LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter )
        {
            string message = $"[{logLevel}] {formatter( state, exception )}";

            if ( string.IsNullOrEmpty( message ) )
                return;

            if ( !IsEnabled( logLevel ) )
                return;

            if ( formatter == null )
                throw new ArgumentNullException( nameof( formatter ) );

            if ( exception != null )
            {
                message += Environment.NewLine + Environment.NewLine + exception;
            }

            outputHelper.WriteLine( message );
        }
    }
}