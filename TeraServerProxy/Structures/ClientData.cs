using TeraCore.Game;
using TeraCore.Sniffing;

namespace TeraServerProxy.Structures
{
    class ClientData : IDisposable
    {
        public Client Client { get; init; }
        public CustomConnectionDecrypter ConnectionDecrypter { get; init; }
        public CustomMessageSplitter MessageSplitter { get; init; }
        //public TcpConnection ServerToClient { get; init; }
        //public TcpConnection? ClientToServer { get; set; }

        public void Dispose()
        {
            MessageSplitter.Dispose();
            ConnectionDecrypter.Dispose();
        }
    }
}
