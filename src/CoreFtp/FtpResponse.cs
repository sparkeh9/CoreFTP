namespace CoreFtp
{
    using Enum;

    public class FtpResponse
    {
        public FtpStatusCode FtpStatusCode { get; set; }
        public string ResponseMessage { get; set; }
    }
}