namespace CoreFtp.Tests.Integration
{
    using System;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public static class Program
    {
        public static FtpConfiguration FtpConfiguration;
        public static ILoggerFactory LoggerFactory;

        public static void Main( string[] args )
        {
            Initialise();
        }

        public static void Initialise()
        {
            LoggerFactory = new LoggerFactory()
                .AddConsole()
                .AddDebug( LogLevel.Debug );

            var builder = new ConfigurationBuilder()
                .SetBasePath( AppContext.BaseDirectory )
                .AddJsonFile( "appsettings.json", true, true );

            IConfigurationRoot configuration = builder.Build();

            var services = new ServiceCollection();
            services.Configure<FtpConfiguration>( configuration.GetSection( "FtpCredentials" ) );
            services.AddOptions();

            var serviceProvider = services.BuildServiceProvider();

            FtpConfiguration = serviceProvider.GetService<IOptions<FtpConfiguration>>().Value;
        }
    }
}