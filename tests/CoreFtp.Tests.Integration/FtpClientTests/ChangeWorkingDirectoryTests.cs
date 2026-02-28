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

    public class ChangeWorkingDirectoryTests : IDisposable
    {
        private readonly ILogger _logger;
        private readonly TcpListener _listener;
        private readonly int _port;

        public ChangeWorkingDirectoryTests(ITestOutputHelper outputHelper)
        {
            var factory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                builder.AddProvider(new XunitLoggerProvider(outputHelper)));
            _logger = factory.CreateLogger<ChangeWorkingDirectoryTests>();

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
        public async Task ChangeWorkingDirectoryAsync_DoesNotThrow_WhenPwdResponseLacksQuotes()
        {
            var serverTask = RunFakeFtpServer(async (reader, writer) =>
            {
                // Initial greeting
                await writer.WriteLineAsync("220 Welcome");

                // USER
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("331 Password required");

                // PASS
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("230 Logged in");

                // FEAT
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("211-Features:");
                await writer.WriteLineAsync(" UTF8");
                await writer.WriteLineAsync("211 End");

                // OPTS UTF8 ON
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("200 OK");

                // TYPE I
                var typeCommand = await reader.ReadLineAsync();
                await writer.WriteLineAsync("200 OK");

                // CWD /
                var startingCwd = await reader.ReadLineAsync();
                await writer.WriteLineAsync("250 CWD OK");
                // PWD (checking CWD /)
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("257 \"/\"");

                // == ACTUALLY READY FOR TEST ==

                // Client sends CWD /my/folder
                var testCwdCmd = await reader.ReadLineAsync();
                await writer.WriteLineAsync("250 OK");

                // Client sends PWD
                var testPwdCmd = await reader.ReadLineAsync();

                // BUG: Return PWD response WITHOUT quotes:
                await writer.WriteLineAsync("257 /my/folder is the current directory");
            });

            var config = new FtpClientConfiguration
            {
                Host = "localhost",
                Port = _port,
                Username = "test",
                Password = "pwd"
            };

            using (var client = new FtpClient(config) { Logger = _logger })
            {
                await client.LoginAsync();

                // It should successfully set WorkingDirectory without throwing!
                await client.ChangeWorkingDirectoryAsync("/my/folder");
                client.WorkingDirectory.Should().Be("/my/folder");
            }

            await serverTask;
        }
    }
}
