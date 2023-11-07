﻿using System.Linq;
using TeraCore.PacketLog;
using TeraCore.Sniffing.Crypt;

namespace TeraCore.Sniffing
{
    public class ConnectionDecrypter : IDisposable
    {
        private readonly MemoryStream _client = new();
        private readonly MemoryStream _server = new();
        private Session? _session;
        private bool _isDisposed = false;

        public event Action<byte[]>? ClientToServerDecrypted;
        public event Action<byte[]>? ServerToClientDecrypted;

        public bool Initialized => _session != null;

        protected virtual void OnClientToServerDecrypted(byte[] data)
        {
            ClientToServerDecrypted?.Invoke(data);
        }

        protected virtual void OnServerToClientDecrypted(byte[] data)
        {
            ServerToClientDecrypted?.Invoke(data);
        }

        private void TryInitialize()
        {
            if (Initialized)
                throw new InvalidOperationException("Already initalized");

            if (_client.Length < 256 + 4 || _server.Length < 256 + 4)
                return;

            _server.Position = 0;
            _client.Position = 0;

            var magicBytes = _server.ReadBytes(4);
            if (!magicBytes.SequenceEqual(new byte[] {1, 0, 0, 0}))
                throw new FormatException("Not a Tera connection");

            var clientKey1 = _client.ReadBytes(128);
            var clientKey2 = _client.ReadBytes(128);
            var serverKey1 = _server.ReadBytes(128);
            var serverKey2 = _server.ReadBytes(128);

            var session = new Session(clientKey1, clientKey2, serverKey1, serverKey2);
            var checkVersion = _client.ReadBytes(4);
            var checkNew = checkVersion.ToArray();
            session.Decrypt(checkNew);

            if (checkNew[2] == 0xbc && checkNew[3] == 0x4d)
            {
                OnClientToServerDecrypted(checkNew);
                _session = session;
            }
            else
                throw new FormatException("Failed to decrypt");

            ClientToServer(_client.ReadBytes((int)(_client.Length - _client.Position)));
            ServerToClient(_server.ReadBytes((int)(_server.Length - _server.Position)));

            Dispose();
        }

        public void Skip(MessageDirection direction, int needToSkip)
        {
            if (Initialized && needToSkip > 0)
            {
                var skip = new byte[needToSkip];
                switch (direction)
                {
                    case MessageDirection.ServerToClient:
                        _session.Encrypt(skip);
                        break;
                    case MessageDirection.ClientToServer:
                        _session.Decrypt(skip);
                        break;
                    default:
                        return;
                };
            }
        }

        public void ClientToServer(ArraySegment<byte> data, int needToSkip = 0)
        {
            if (data.Array is null)
                return;

            if (Initialized)
            {
                if (needToSkip > 0)
                {
                    var skip = new byte[needToSkip];
                    _session.Decrypt(skip);
                }
                var result = data.ToArray();
                _session.Decrypt(result);

                OnClientToServerDecrypted(result);
            }
            else
            {
                _client.Write(data.Array, data.Offset, data.Count);
                TryInitialize();
            }
        }

        public void ServerToClient(ArraySegment<byte> data, int needToSkip = 0)
        {
            if (data.Array is null)
                return;

            if (Initialized)
            {
                if (needToSkip > 0)
                {
                    var skip = new byte[needToSkip];
                    _session.Encrypt(skip);
                }
                var result = data.ToArray();
                _session.Encrypt(result);
                OnServerToClientDecrypted(result);
            }
            else
            {
                _server.Write(data.Array, data.Offset, data.Count);
                TryInitialize();
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _client.Dispose();
                _server.Dispose();
                _isDisposed = true;
            }
        }
    }
}