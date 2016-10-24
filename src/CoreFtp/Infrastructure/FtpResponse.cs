namespace CoreFtp.Infrastructure
{
    using Enum;

    public class FtpResponse
    {
        public FtpStatusCode FtpStatusCode { get; set; }
        public string ResponseMessage { get; set; }
        public string[] Data { get; set; }

        public static FtpResponse EmptyResponse = new FtpResponse
        {
            ResponseMessage = "No response was received",
            FtpStatusCode = FtpStatusCode.Undefined
        };
    }
}