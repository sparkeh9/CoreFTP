namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class When_creating_a_deep_folder_from_root
    {
        [ Fact ]
        public async Task Should_create_directory_structure_recursively()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                var guid = Guid.NewGuid();
                await sut.LoginAsync();
                await sut.SetClientName( nameof( Should_create_directory_structure_recursively ) );

                await sut.CreateDirectoryAsync( $"toplevel/{guid}/abc/123" );

                ( await sut.ListDirectoriesAsync() ).ToList()
                                                    .Any( x => x.Name == "toplevel" )
                                                    .Should().BeTrue();

                await sut.ChangeWorkingDirectoryAsync( "toplevel" );

                ( await sut.ListDirectoriesAsync() ).ToList()
                                                    .Any( x => x.Name == guid.ToString() )
                                                    .Should().BeTrue();

                await sut.ChangeWorkingDirectoryAsync( guid.ToString() );

                ( await sut.ListDirectoriesAsync() ).ToList()
                                                    .Any( x => x.Name == "abc" )
                                                    .Should().BeTrue();
            }
        }
    }
}