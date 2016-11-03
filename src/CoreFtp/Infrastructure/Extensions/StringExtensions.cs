namespace CoreFtp.Infrastructure.Extensions
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using Enum;
    using Infrastructure;

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

        public static FtpNodeType ToNodeType( this string operand )
        {
            switch ( operand )
            {
                case "dir":
                    return FtpNodeType.Directory;
                case "file":
                    return FtpNodeType.File;
            }

            return FtpNodeType.SymbolicLink;
        }

        public static FtpNodeInformation ToFtpNode( this string operand )
        {
            var dictionary = operand.Split( ';' )
                                    .Select( s => s.Split( '=' ) )
                                    .ToDictionary( strings => strings.Length == 2
                                                       ? strings[ 0 ]
                                                       : "name",
                                                   strings => strings.Length == 2
                                                       ? strings[ 1 ]
                                                       : strings[ 0 ] );

            return new FtpNodeInformation
            {
                NodeType = dictionary.GetValueOrDefault( "type" ).Trim().ToNodeType(),
                Name = dictionary.GetValueOrDefault( "name" ).Trim(),
                Size = dictionary.GetValueOrDefault( "size" ).ParseOrDefault(),
                DateModified = dictionary.GetValueOrDefault( "modify" ).ParseExactOrDefault( "yyyyMMddHHmmss" )
            };
        }
    }
}