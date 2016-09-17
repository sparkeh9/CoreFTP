namespace CoreFtp
{
    using System;

    public class FtpClientConfiguration
    {
        public int TimeoutSeconds { get; set; } = 120;
        public int BufferSize { get; set; } = 512;
        public int Port { get; set; } = 21;
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string BaseDirectory { get; set; } = "/";
    }
}