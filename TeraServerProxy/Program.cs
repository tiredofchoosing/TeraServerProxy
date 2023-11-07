using NetProxy;
using NLog;
using System.Net;
using TeraCore.Game;
using TeraCore.Game.Messages;
using TeraCore.Sniffing;
using TeraServerProxy.MessageProcessor;
using TeraServerProxy.Structures;

namespace TeraServerProxy
{
    internal class Program
    {
        static Dictionary<ushort, string> opCodes;
        static OpCodeNamer opCodeNamer;
        static MessageFactory messageFactory;
        static MessageProcessorFactory messageProcessorFactory;
        static TeraDataPools dataPools;
        static Dictionary<IPEndPoint, ClientData> clientsData;

        static NLog.ILogger logger;

        static readonly string configDir = "Config";
        static readonly object loggerLock = new();

        static void Main(string[] args)
        {
            var nlogConfigFile = Path.Combine(Environment.CurrentDirectory, configDir, "nlog.config");
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(nlogConfigFile);
            logger = LogManager.GetLogger("Main");
            Log(logger.Info, "======================");
            Log(logger.Info, "Init");

            opCodes = new Dictionary<ushort, string>
            {
                { 58604, "S_LOGIN" },
                { 54807, "S_RETURN_TO_LOBBY" },
                { 27768, "S_USER_LEVELUP" },

                { 48376, "S_ADD_INTER_PARTY_MATCH_POOL" },
                { 42469, "S_DEL_INTER_PARTY_MATCH_POOL" },
                { 21623, "S_MODIFY_INTER_PARTY_MATCH_POOL" },

                //{ 23845, "C_REGISTER_PARTY_INFO" },
                //{ 54412, "C_UNREGISTER_PARTY_INFO" },
                //{ 45446, "S_EXIT" },
            };

            opCodeNamer = new(opCodes);
            messageFactory = new(opCodeNamer);

            clientsData = new();
            dataPools = new();
            messageProcessorFactory = new(dataPools, logger);

            Proxy.ClientDataReceived += ClientDataReceived;
            Proxy.ServerDataReceived += ServerDataReceived;
            Proxy.OnNewConnection += OnNewConnection;
            Proxy.OnEndConnection += OnEndConnection;
            Proxy.Run();
        }

        private static void ClientDataReceived(IPEndPoint clientEndPoint, ArraySegment<byte> data)
        {
            var dec = clientsData[clientEndPoint].ConnectionDecrypter;
            dec.ClientToServer(data);
        }

        private static void ServerDataReceived(IPEndPoint clientEndPoint, ArraySegment<byte> data)
        {
            var dec = clientsData[clientEndPoint].ConnectionDecrypter;
            dec.ServerToClient(data);
        }

        private static void OnNewConnection(IPEndPoint clientEndPoint)
        {
            var client = new Client(clientEndPoint);

            var decrypter = new CustomConnectionDecrypter(clientEndPoint);
            decrypter.CustomClientToServerDecrypted += ClientToServerDecrypted;
            decrypter.CustomServerToClientDecrypted += ServerToClientDecrypted;

            var messageSplitter = new CustomMessageSplitter(clientEndPoint);
            messageSplitter.MessageClientReceived += TeraMessageReceived;

            var clientData = new ClientData()
            {
                Client = client,
                ConnectionDecrypter = decrypter,
                MessageSplitter = messageSplitter
            };
            clientsData.Add(clientEndPoint, clientData);
            dataPools.Add(client);
        }

        private static void OnEndConnection(IPEndPoint clientEndPoint)
        {
            if (clientsData.TryGetValue(clientEndPoint, out var clientData))
            {
                clientsData.Remove(clientEndPoint);
                dataPools.Remove(clientData.Client);

                if (clientData.ConnectionDecrypter != null)
                {
                    clientData.ConnectionDecrypter.CustomClientToServerDecrypted -= ClientToServerDecrypted;
                    clientData.ConnectionDecrypter.CustomServerToClientDecrypted -= ServerToClientDecrypted;
                }
                if (clientData.MessageSplitter != null)
                {
                    clientData.MessageSplitter.MessageClientReceived -= TeraMessageReceived;
                }
                clientData.Dispose();
            }
        }

        private static void ClientToServerDecrypted(IPEndPoint clientEndPoint, byte[] data)
        {
            var splitter = clientsData[clientEndPoint].MessageSplitter;
            splitter.ClientToServer(DateTime.UtcNow, data);
        }

        private static void ServerToClientDecrypted(IPEndPoint clientEndPoint, byte[] data)
        {
            var splitter = clientsData[clientEndPoint].MessageSplitter;
            splitter.ServerToClient(DateTime.UtcNow, data);
        }

        private static void TeraMessageReceived(IPEndPoint clientEndPoint, Message message)
        {
            var client = clientsData[clientEndPoint]?.Client;
            var msg = messageFactory.Create(message);
            if (msg is null || client is null)
                return;

            try
            {
                ProcessParsedMessage(msg, client);
            }
            catch (Exception e)
            {
                Log(logger.Error, $"{client}|Error while processing {msg.GetType()}\n{e.Message}");
            }
        }

        private static void ProcessParsedMessage(ParsedMessage message, Client client)
        {
            var processor = messageProcessorFactory.Create(message, client);
            processor.Process();
        }

        private static void Log(Action<string> action, string message)
        {
            lock (loggerLock)
            {
                action(message);
            }
        }
    }
}