namespace CoreFtp.Attributes
{
    using System;

    [ AttributeUsage( AttributeTargets.All ) ]
    public class FtpCommandValueAttribute : Attribute
    {
        public string Command { get; set; }

        public FtpCommandValueAttribute( string command )
        {
            Command = command;
        }
    }
}