namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using Microsoft.Extensions.Logging;

    public abstract class TestBase
    {
        protected ILogger Logger { get; set; }

        protected TestBase()
        {
            Program.Initialise();
            Logger = Program.LoggerFactory.CreateLogger( GetType().Name );
        }
    }
}