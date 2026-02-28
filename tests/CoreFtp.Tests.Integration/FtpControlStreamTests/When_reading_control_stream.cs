namespace CoreFtp.Tests.Integration.FtpControlStreamTests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using FluentAssertions;
    using Infrastructure.Stream;
    using Xunit;

    public class When_reading_control_stream
    {
        private sealed class TestStream : FtpControlStream
        {
            public TestStream() : base( new FtpClientConfiguration(), null ) { }

            public void SetBaseStream( Stream stream )
            {
                BaseStream = stream;
            }

            public string TestReadLine( Encoding encoding, CancellationToken token = default( CancellationToken ) )
                => ReadLine( encoding, token );
        }

        private static MemoryStream ToStream( string content, Encoding encoding = null )
        {
            return new MemoryStream( ( encoding ?? Encoding.ASCII ).GetBytes( content ) );
        }

        [ Fact ]
        public void ReadLine_returns_null_when_stream_is_empty()
        {
            var sut = new TestStream();
            sut.SetBaseStream( new MemoryStream() );

            var result = sut.TestReadLine( Encoding.ASCII );

            result.Should().BeNull();
        }

        [ Fact ]
        public void ReadLine_returns_line_content_when_terminated_by_lf()
        {
            var sut = new TestStream();
            sut.SetBaseStream( ToStream( "220 Welcome\n" ) );

            var result = sut.TestReadLine( Encoding.ASCII );

            result.Should().Be( "220 Welcome" );
        }

        [ Fact ]
        public void ReadLine_strips_cr_from_crlf_line_ending()
        {
            var sut = new TestStream();
            sut.SetBaseStream( ToStream( "220 Welcome\r\n" ) );

            var result = sut.TestReadLine( Encoding.ASCII );

            result.Should().Be( "220 Welcome" );
        }

        [ Fact ]
        public void ReadLine_returns_null_for_incomplete_line_without_lf()
        {
            var sut = new TestStream();
            sut.SetBaseStream( ToStream( "220 Welcome" ) );

            var result = sut.TestReadLine( Encoding.ASCII );

            result.Should().BeNull();
        }

        [ Fact ]
        public void ReadLine_throws_when_encoding_is_null()
        {
            var sut = new TestStream();
            sut.SetBaseStream( new MemoryStream() );

            Action act = () => sut.TestReadLine( null );

            act.ShouldThrow<ArgumentNullException>();
        }

        [ Fact ]
        public void ReadLine_throws_when_cancellation_already_requested()
        {
            var sut = new TestStream();
            sut.SetBaseStream( ToStream( "220 Welcome\n" ) );
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Action act = () => sut.TestReadLine( Encoding.ASCII, cts.Token );

            act.ShouldThrow<OperationCanceledException>();
        }

        [ Fact ]
        public void ReadByte_returns_minus_one_when_network_stream_is_null()
        {
            var sut = new TestStream();

            var result = sut.ReadByte();

            result.Should().Be( -1 );
        }

        [ Fact ]
        public void ReadByte_returns_byte_value_when_data_available()
        {
            var sut = new TestStream();
            sut.SetBaseStream( ToStream( "A" ) );

            var result = sut.ReadByte();

            result.Should().Be( (int) 'A' );
        }

        [ Fact ]
        public void ReadByte_returns_minus_one_at_end_of_stream()
        {
            var sut = new TestStream();
            sut.SetBaseStream( new MemoryStream() );

            var result = sut.ReadByte();

            result.Should().Be( -1 );
        }
    }
}
