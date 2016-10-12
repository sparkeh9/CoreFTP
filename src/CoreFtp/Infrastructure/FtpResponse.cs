namespace CoreFtp.Infrastructure
{
    using Enum;

    public class FtpResponse
    {
        public FtpStatusCode FtpStatusCode { get; set; }
        public string ResponseMessage { get; set; }
    }
}