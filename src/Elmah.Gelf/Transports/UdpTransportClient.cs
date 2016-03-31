using System.Net;
using System.Net.Sockets;

namespace Elmah.Gelf.Transport
{
    public class UdpTransportClient : ITransportClient
    {
        public void Send(byte[] datagram, int bytes, IPEndPoint ipEndPoint)
        {
            using (var udpClient = new UdpClient())
            {
                udpClient.Send(datagram, bytes, ipEndPoint);
            }
        }
    }
}