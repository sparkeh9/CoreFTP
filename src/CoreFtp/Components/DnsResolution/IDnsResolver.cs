namespace CoreFtp.Components.DnsResolution
{
    using System.Net;
    using System.Threading.Tasks;
    using Enum;

    public interface IDnsResolver
    {
        Task<IPEndPoint> ResolveAsync( string endpoint, int port, IpVersion ipVersion );
    }
}