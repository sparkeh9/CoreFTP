namespace CoreFtp.Tests.Integration.Helpers
{
    using System.IO;

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