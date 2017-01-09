namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System;
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public abstract class TestBase : IDisposable
    {
        protected ILogger Logger { get; set; }

        protected TestBase( ITestOutputHelper outputHelper = null )
        {
            Program.Initialise( outputHelper );
            Logger = Program.LoggerFactory.CreateLogger( GetType().Name );
        }

        protected virtual void Dispose( bool disposing )
        {
            if ( disposing )
            {
                Logger = null;
            }
        }

        public void Dispose()
        {
            Dispose( true );
        }
    }
}