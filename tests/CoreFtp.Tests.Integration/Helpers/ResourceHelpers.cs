namespace CoreFtp.Tests.Integration.Helpers
{
    using System.IO;
    using System.Threading.Tasks;
    using Enum;

    public static class ResourceHelpers
    {
        public static DirectoryInfo GetResourceDirectoryInfo( string directory = "" )
        {
            return new DirectoryInfo( $"{Directory.GetCurrentDirectory()}/Resources/{directory}" );
        }

        public static FileInfo GetResourceFileInfo( string filename )
        {
            return new FileInfo( $"{Directory.GetCurrentDirectory()}/Resources/{filename}" );
        }

        public static async Task CreateTestResourceWithNameAsync( this FtpClient ftpClient, string resourceName, string asFileName )
        {
            var resourceFileInfo = GetResourceFileInfo( resourceName );
            await ftpClient.SetTransferMode( FtpTransferMode.Binary );
            using ( var writeStream = await ftpClient.OpenFileWriteStreamAsync( asFileName ) )
            {
                var fileReadStream = resourceFileInfo.OpenRead();
                await fileReadStream.CopyToAsync( writeStream );
            }
        }

        public static string GetTempFilePath()
        {
            return Path.GetTempPath();
        }

        public static FileInfo GetTempFileInfo()
        {
            return new FileInfo( Path.GetTempFileName() );
        }
    }
}