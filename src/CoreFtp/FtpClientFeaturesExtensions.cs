namespace CoreFtp
{
    using System.Linq;

    public static class FtpClientFeaturesExtensions
    {
        public static bool UsesMlsd( this FtpClient operand )
        {
            return operand.Features != null && operand.Features.Any( x => x == "MLSD" );
        }
    }
}