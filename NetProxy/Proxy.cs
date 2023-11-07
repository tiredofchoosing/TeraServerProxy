using System.Net;
using System.Text.Json;

namespace NetProxy
{
    public static class Proxy
    {
        public static event Action<IPEndPoint, ArraySegment<byte>>? ClientDataReceived;
        public static event Action<IPEndPoint, ArraySegment<byte>>? ServerDataReceived;
        public static event Action<IPEndPoint>? OnNewConnection;
        public static event Action<IPEndPoint>? OnEndConnection;

        public static void Run(string proxyPath = "config.json")
        {
            try
            {
                var configJson = File.ReadAllText(proxyPath);
                var options = new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var configs = JsonSerializer.Deserialize<Dictionary<string, ProxyConfig>>(configJson, options);
                if (configs == null)
                    throw new Exception("configs is null");

                var tasks = configs.SelectMany(c => ProxyFromConfig(c.Key, c.Value));
                Task.WhenAll(tasks).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred : {ex}");
            }
        }

        private static IEnumerable<Task> ProxyFromConfig(string proxyName, ProxyConfig proxyConfig)
        {
            var forwardPort = proxyConfig.ForwardPort;
            var localPort = proxyConfig.LocalPort;
            var forwardIp = proxyConfig.ForwardIp;
            var localIp = proxyConfig.LocalIp;
            var protocol = proxyConfig.Protocol;

            try
            {
                if (forwardIp == null)
                {
                    throw new Exception("forwardIp is null");
                }
                if (!forwardPort.HasValue)
                {
                    throw new Exception("forwardPort is null");
                }
                if (!localPort.HasValue)
                {
                    throw new Exception("localPort is null");
                }
                if (protocol != "tcp")
                {
                    throw new Exception($"protocol is not supported {protocol}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start {proxyName} : {ex.Message}");
                throw;
            }

            bool protocolHandled = false;
            if (protocol == "tcp")
            {
                protocolHandled = true;
                Task task;
                try
                {
                    var proxy = new TcpProxy();
                    proxy.ClientDataReceived += ClientDataReceivedEventHandler;
                    proxy.ServerDataReceived += ServerDataReceivedEventHandler;
                    proxy.OnNewConnection += NewConnectionEventHandler;
                    proxy.OnEndConnection += EndConnectionEventHandler;
                    task = proxy.Start(forwardIp, forwardPort.Value, localPort.Value, localIp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to start {proxyName} : {ex.Message}");
                    throw;
                }

                yield return task;
            }

            if (!protocolHandled)
            {
                throw new InvalidOperationException($"protocol not supported {protocol}");
            }
        }

        private static void ClientDataReceivedEventHandler(IPEndPoint client, ArraySegment<byte> data)
        {
            ClientDataReceived?.Invoke(client, data);
        }

        private static void ServerDataReceivedEventHandler(IPEndPoint client, ArraySegment<byte> data)
        {
            ServerDataReceived?.Invoke(client, data);
        }

        private static void NewConnectionEventHandler(IPEndPoint client)
        {
            OnNewConnection?.Invoke(client);
        }

        private static void EndConnectionEventHandler(IPEndPoint client)
        {
            OnEndConnection?.Invoke(client);
        }
    }

    internal class ProxyConfig
    {
        public string? Protocol { get; set; }
        public ushort? LocalPort { get; set; }
        public string? LocalIp { get; set; }
        public string? ForwardIp { get; set; }
        public ushort? ForwardPort { get; set; }
    }
}