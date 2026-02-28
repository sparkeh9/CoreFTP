namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Shared;
    using Xunit;
    using Xunit.Abstractions;

    public class DisposeTests : IDisposable
    {
        private readonly ILogger _logger;
        private readonly TcpListener _listener;
        private readonly int _port;

        public DisposeTests(ITestOutputHelper outputHelper)
        {
            var factory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                builder.AddProvider(new XunitLoggerProvider(outputHelper)));
            _logger = factory.CreateLogger<DisposeTests>();

            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public void Dispose()
        {
            _listener.Stop();
        }

        private async Task RunFakeFtpServer(Func<StreamReader, StreamWriter, Task> handler)
        {
            using (var client = await _listener.AcceptTcpClientAsync())
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            using (var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
            {
                await handler(reader, writer);
            }
        }

        [Fact]
        public async Task Dispose_ShouldNotThrow_WhenLogOutFails()
        {
            var serverTask = RunFakeFtpServer(async (reader, writer) =>
            {
                await writer.WriteLineAsync("220 Welcome");
                await reader.ReadLineAsync(); // USER
                await writer.WriteLineAsync("331 Password required");
                await reader.ReadLineAsync(); // PASS
                await writer.WriteLineAsync("230 Logged in");
                await reader.ReadLineAsync(); // FEAT
                await writer.WriteLineAsync("211-Features:");
                await writer.WriteLineAsync(" UTF8");
                await writer.WriteLineAsync("211 End");
                await reader.ReadLineAsync(); // OPTS UTF8
                await writer.WriteLineAsync("200 OK");
                await reader.ReadLineAsync(); // TYPE I
                await writer.WriteLineAsync("200 OK");
                await reader.ReadLineAsync(); // CWD /
                await writer.WriteLineAsync("250 CWD OK");
                await reader.ReadLineAsync(); // PWD
                await writer.WriteLineAsync("257 \"/\"");

                // Simulate drop before QUIT is processed
                var quitCmd = await reader.ReadLineAsync();
                writer.BaseStream.Close();
            });

            var config = new FtpClientConfiguration
            {
                Host = "localhost",
                Port = _port,
                Username = "test",
                Password = "pwd"
            };

            var client = new FtpClient(config) { Logger = _logger };
            await client.LoginAsync();

            // Dispose calls Task.WaitAny(LogOutAsync()) which shouldn't throw,
            // but under the hood, we are fixing it so Dispose catches the exception
            // properly. Before the fix, WaitAny would swallow it *if* it returned immediately,
            // but the bug is it often leaves the task hanging.
            // By wrapping in an action we ensure no exception is thrown out of Dispose.
            Action act = () => client.Dispose();
            act.Should().NotThrow();

            await serverTask;
        }
    }
}
