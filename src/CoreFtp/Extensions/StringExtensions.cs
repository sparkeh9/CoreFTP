namespace CoreFtp.Extensions
{
    using System.Text.RegularExpressions;

    public static class StringExtensions
    {
        public static bool IsNullOrEmpty( this string operand )
        {
            return string.IsNullOrEmpty( operand );
        }

        public static bool IsNullOrWhiteSpace( this string operand )
        {
            return string.IsNullOrWhiteSpace( operand );
        }

        public static int? ExtractEpsvPortNumber( this string operand )
        {
            var regex = new Regex( @"(?:\|)(?<PortNumber>\d+)(?:\|)", RegexOptions.Compiled );

            var match = regex.Match( operand );

            if ( !match.Success )
                return null;

            return int.Parse( match.Groups[ "PortNumber" ].Value );
        }
    }
}