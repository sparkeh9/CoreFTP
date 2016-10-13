namespace CoreFtp.Components.DirectoryListing.Parser
{
    using System.IO;
    using System.Text.RegularExpressions;
    using Enum;
    using Infrastructure;

    public class UnixDirectoryParser : IParser
    {
        private readonly Regex unixRegex = new Regex( @"(?<permissions>.+)\s+" +
                                                      @"(?<objectcount>\d+)\s+" +
                                                      @"(?<user>.+)\s+" +
                                                      @"(?<group>.+)\s+" +
                                                      @"(?<size>\d+)\s+" +
                                                      @"(?<modify>\w+\s+\d+\s+\d+:\d+|\w+\s+\d+\s+\d+)\s" +
                                                      @"(?<name>.*)$", RegexOptions.Compiled );

        public bool Test( string testString )
        {
            return unixRegex.Match( testString ).Success;
        }

        public FtpNodeInformation Parse( string line )
        {
            var matches = unixRegex.Match( line );

            if ( !matches.Success )
                return null;

            var node = new FtpNodeInformation
            {
                NodeType = DetermineNodeType( matches.Groups[ "permissions" ] )
            };


            return null;
        }


        private FtpNodeType DetermineNodeType( Capture permissions )
        {
            // No permissions means we can't determine the node type
            if ( permissions.Value.Length == 0 )
                throw new InvalidDataException( "No permissions found" );

            switch ( permissions.Value[ 0 ] )
            {
                case 'd':
                    return FtpNodeType.Directory;
                case '-':
                case 's':
                    return FtpNodeType.File;
                case 'l':
                    return FtpNodeType.SymbolicLink;
                default:
                    throw new InvalidDataException( "Unexpected data format" );
            }
        }
    }
}