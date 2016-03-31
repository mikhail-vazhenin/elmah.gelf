using System.Net;
namespace Elmah.Gelf.Transport
{
    public interface ITransport
    {
        string Scheme { get; }
        void Send(IPEndPoint target, string message);
    }
}