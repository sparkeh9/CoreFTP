namespace CoreFtp
{
    using Enum;
    using Extensions;

    public class FtpCommandEnvelope
    {
        public FtpCommand FtpCommand { get; set; }
        public string Data { get; set; }

        public string GetCommandString()
        {
            string command = FtpCommand.ToCommandString();

            return Data.IsNullOrEmpty()
                ? command
                : $"{command} {Data}";
        }
    }
}