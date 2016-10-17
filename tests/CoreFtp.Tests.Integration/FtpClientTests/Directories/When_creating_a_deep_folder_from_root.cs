namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class When_creating_a_deep_folder_from_root
    {
        public When_creating_a_deep_folder_from_root()
        {
            Program.Initialise();
        }

        [ Fact ]
        public async Task Should_create_directory_structure_recursively()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                sut.Logger = Program.LoggerFactory.CreateLogger( "test" );
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