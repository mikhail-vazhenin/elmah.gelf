using System.Net;

namespace Elmah.Gelf.Transport
{
    public interface ITransportClient
    {
        void Send(byte[] datagram, int bytes, IPEndPoint ipEndPoint);
    }
}