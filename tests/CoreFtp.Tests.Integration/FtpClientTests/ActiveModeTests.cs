namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Shared;
    using Xunit;
    using Xunit.Abstractions;

    public class ActiveModeTests : IDisposable
    {
        private readonly ILogger _logger;
        private readonly TcpListener _controlListener;
        private readonly int _controlPort;

        public ActiveModeTests(ITestOutputHelper outputHelper)
        {
            var factory = LoggerFactory.Create(builder =>
                builder.AddProvider(new XunitLoggerProvider(outputHelper)));
            _logger = factory.CreateLogger<ActiveModeTests>();

            _controlListener = new TcpListener(IPAddress.Loopback, 0);
            _controlListener.Start();
            _controlPort = ((IPEndPoint)_controlListener.LocalEndpoint).Port;
        }

        public void Dispose()
        {
            _controlListener.Stop();
        }

        private async Task RunFakeFtpServer(Func<StreamReader, StreamWriter, Task> handler)
        {
            using (var client = await _controlListener.AcceptTcpClientAsync())
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            using (var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
            {
                await handler(reader, writer);
            }
        }

        private static (string ip, int port) ParsePortCommand(string portArgs)
        {
            var parts = portArgs.Split(',');
            string ip = $"{parts[0]}.{parts[1]}.{parts[2]}.{parts[3]}";
            int port = int.Parse(parts[4]) * 256 + int.Parse(parts[5]);
            return (ip, port);
        }

        [Fact]
        public async Task ListAllAsync_ActiveMode_ReturnsDirectoryListing()
        {
            var serverTask = RunFakeFtpServer(async (reader, writer) =>
            {
                // Greeting
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
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("200 OK");

                // CWD /
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("250 CWD OK");
                // PWD
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("257 \"/\"");

                // PORT command
                var portLine = await reader.ReadLineAsync();
                var portArgs = portLine.Substring("PORT ".Length);
                var (ip, port) = ParsePortCommand(portArgs);
                await writer.WriteLineAsync("200 PORT command successful");

                // LIST
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("150 Opening data connection");

                // Server connects back to client's data listener
                using (var dataClient = new TcpClient())
                {
                    await dataClient.ConnectAsync(ip, port);
                    using (var dataStream = dataClient.GetStream())
                    using (var dataWriter = new StreamWriter(dataStream, Encoding.ASCII) { AutoFlush = true })
                    {
                        await dataWriter.WriteLineAsync("-rw-r--r-- 1 user group 1024 Jan 01 00:00 file1.txt");
                        await dataWriter.WriteLineAsync("drwxr-xr-x 2 user group 4096 Jan 01 00:00 subdir");
                        await dataWriter.WriteLineAsync("-rw-r--r-- 1 user group 2048 Jan 01 00:00 file2.log");
                    }
                }

                await writer.WriteLineAsync("226 Transfer complete");
            });

            var config = new FtpClientConfiguration
            {
                Host = "localhost",
                Port = _controlPort,
                Username = "test",
                Password = "pwd",
                DataConnectionType = FtpDataConnectionType.Active,
                ActiveExternalIp = "127.0.0.1"
            };

            using (var client = new FtpClient(config) { Logger = _logger })
            {
                await client.LoginAsync();

                var nodes = await client.ListAllAsync();

                nodes.Should().HaveCount(3);
                nodes.Count(n => n.NodeType == FtpNodeType.File).Should().Be(2);
                nodes.Count(n => n.NodeType == FtpNodeType.Directory).Should().Be(1);
                nodes.Any(n => n.Name == "file1.txt").Should().BeTrue();
                nodes.Any(n => n.Name == "subdir").Should().BeTrue();
                nodes.Any(n => n.Name == "file2.log").Should().BeTrue();
            }

            await serverTask;
        }

        [Fact]
        public async Task ListFilesAsync_ActiveMode_ReturnsOnlyFiles()
        {
            var serverTask = RunFakeFtpServer(async (reader, writer) =>
            {
                await writer.WriteLineAsync("220 Welcome");
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("331 Password required");
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("230 Logged in");
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("211-Features:");
                await writer.WriteLineAsync(" UTF8");
                await writer.WriteLineAsync("211 End");
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("200 OK");
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("200 OK");
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("250 CWD OK");
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("257 \"/\"");

                // PORT command
                var portLine = await reader.ReadLineAsync();
                var portArgs = portLine.Substring("PORT ".Length);
                var (ip, port) = ParsePortCommand(portArgs);
                await writer.WriteLineAsync("200 PORT command successful");

                // LIST
                await reader.ReadLineAsync();
                await writer.WriteLineAsync("150 Opening data connection");

                using (var dataClient = new TcpClient())
                {
                    await dataClient.ConnectAsync(ip, port);
                    using (var dataStream = dataClient.GetStream())
                    using (var dataWriter = new StreamWriter(dataStream, Encoding.ASCII) { AutoFlush = true })
                    {
                        await dataWriter.WriteLineAsync("-rw-r--r-- 1 user group 1024 Jan 01 00:00 readme.md");
                        await dataWriter.WriteLineAsync("drwxr-xr-x 2 user group 4096 Jan 01 00:00 docs");
                    }
                }

                await writer.WriteLineAsync("226 Transfer complete");
            });

            var config = new FtpClientConfiguration
            {
                Host = "localhost",
                Port = _controlPort,
                Username = "test",
                Password = "pwd",
                DataConnectionType = FtpDataConnectionType.Active,
                ActiveExternalIp = "127.0.0.1"
            };

            using (var client = new FtpClient(config) { Logger = _logger })
            {
                await client.LoginAsync();

                var files = await client.ListFilesAsync();

                files.Should().HaveCount(1);
                files[0].Name.Should().Be("readme.md");
                files[0].NodeType.Should().Be(FtpNodeType.File);
            }

            await serverTask;
        }
    }
}
