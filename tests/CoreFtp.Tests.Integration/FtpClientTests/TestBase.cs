namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public abstract class TestBase
    {
        protected ILogger Logger { get; set; }

        protected TestBase( ITestOutputHelper outputHelper = null )
        {
            Program.Initialise( outputHelper );
            Logger = Program.LoggerFactory.CreateLogger( GetType().Name );
        }
    }
}