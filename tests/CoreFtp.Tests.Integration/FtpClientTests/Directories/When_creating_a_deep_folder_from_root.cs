﻿namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Xunit;
    using Xunit.Abstractions;

    public class When_creating_a_deep_folder_from_root : TestBase
    {
        public When_creating_a_deep_folder_from_root( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_create_directory_structure_recursively( FtpEncryption encryption )
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = encryption == FtpEncryption.Implicit
                    ? 990
                    : Program.FtpConfiguration.Port,
                EncryptionType = encryption,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;

                var guid = Guid.NewGuid().ToString();
                await sut.LoginAsync();
                await sut.SetClientName( nameof( Should_create_directory_structure_recursively ) );

                await sut.CreateDirectoryAsync( $"{guid}/abc/123" );

                ( await sut.ListDirectoriesAsync() ).ToList()
                                                    .Any( x => x.Name == guid )
                                                    .Should().BeTrue();

                await sut.ChangeWorkingDirectoryAsync( guid );

                ( await sut.ListDirectoriesAsync() ).ToList()
                                                    .Any( x => x.Name == "abc" )
                                                    .Should().BeTrue();
                await sut.ChangeWorkingDirectoryAsync( "/" );

                await sut.DeleteDirectoryAsync( $"/{guid}/abc/123" );
                await sut.DeleteDirectoryAsync( $"/{guid}/abc" );
                await sut.DeleteDirectoryAsync( guid );
            }
        }
    }
}