namespace CoreFtp.Tests.Integration.Helpers
{
    using System.IO;

    public static class BinaryDataExtensions
    {
        public static Stream ToByteStream( this string operand )
        {
            var memoryStream = new MemoryStream();
            var writer = new StreamWriter( memoryStream );
            writer.Write( operand );
            writer.Flush();
            memoryStream.Position = 0;
            return memoryStream;
        }

        public static byte[] ToBytes( this string operand )
        {
            return System.Text.Encoding.UTF8.GetBytes( operand );
        }
    }
}