namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Helpers;
    using Xunit;
    using Xunit.Abstractions;

    public class When_connecting_to_a_tls_encrypted_ftp_server : TestBase
    {
        public When_connecting_to_a_tls_encrypted_ftp_server( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Fact ]
        public async Task Should_explicitly_initiate_tls_after_connecting_to_standard_port()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = Program.FtpConfiguration.Port,
                EncryptionType = FtpEncryption.Explicit,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;

                await sut.LoginAsync();
                sut.IsEncrypted.Should().BeTrue();
                await sut.LogOutAsync();
            }
        }
        
    }
}