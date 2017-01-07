namespace CoreFtp.Tests.Integration.DnsResolverTests
{
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Components.DnsResolution;
    using Enum;
    using FluentAssertions;
    using Xunit;

    public class When_resolving_an_endpoint
    {
        [ Theory ]
        [ InlineData( "127.0.0.1" ) ]
        [ InlineData( "127.0.0.1:12" ) ]
        [ InlineData( "::1" ) ]
        [ InlineData( "[::1]" ) ]
        [ InlineData( "[::1]:21" ) ]
        [ InlineData( "2001:db8::" ) ]
        public async Task Should_return_ip_endpoint_when_given_ip_address( string ipAddress )
        {
            var sut = new DnsResolver();

            var endpoint = await sut.ResolveAsync( ipAddress, 21 );
            endpoint.Should().BeOfType<IPEndPoint>();
        }

        [ Theory ]
        [ InlineData( "localhost", "127.0.0.1", IpVersion.IpV4, AddressFamily.InterNetwork ) ]
        [ InlineData( "localhost", "::1", IpVersion.IpV6, AddressFamily.InterNetworkV6 ) ]
        [ InlineData( "google-public-dns-a.google.com", "8.8.8.8", IpVersion.IpV4, AddressFamily.InterNetwork ) ]
        public async Task Should_return_ip_endpoint_when_given_hostname( string hostname, string expectedIpAddress, IpVersion ipVersion, AddressFamily addressFamily )
        {
            var sut = new DnsResolver();

            var endpoint = await sut.ResolveAsync( hostname, 21, ipVersion );
            endpoint.Should().BeOfType<IPEndPoint>();

            var ipEndpoint = endpoint;
            ipEndpoint.Address.ToString().Should().Be( expectedIpAddress );
            ipEndpoint.AddressFamily.Should().Be( addressFamily );
        }
    }
}