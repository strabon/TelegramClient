﻿namespace TelegramClient.Core.Network.Tcp
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using TelegramClient.Core.IoC;
    using TelegramClient.Core.Settings;

    [SingleInstance(typeof(ITcpService))]
    internal class TcpService : ITcpService
    {
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);

        private TcpClient _tcpClient;

        public IClientSettings ClientSettings { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<Stream> Receieve()
        {
            await EnsureClientConnected().ConfigureAwait(false);

            return _tcpClient.GetStream();
        }

        public async Task Send(byte[] encodedMessage)
        {
            await EnsureClientConnected().ConfigureAwait(false);
            await _tcpClient.GetStream().WriteAsync(encodedMessage, 0, encodedMessage.Length).ConfigureAwait(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _resetEvent?.Dispose();
                _tcpClient?.Dispose();
            }
        }

        private async Task EnsureClientConnected()
        {
            var session = ClientSettings.Session;

            if (_tcpClient == null)
            {
                _resetEvent.WaitOne();
                if (_tcpClient == null)
                {
                    _tcpClient = new TcpClient();
                    await _tcpClient.ConnectAsync(session.ServerAddress, session.Port).ConfigureAwait(false);
                }

                _resetEvent.Set();
            }
            else
            {
                if (!_tcpClient.Connected)
                {
                    _resetEvent.WaitOne();
                    var endpoint = (IPEndPoint)_tcpClient.Client.RemoteEndPoint;

                    if (!_tcpClient.Connected || endpoint.Address.ToString() != session.ServerAddress || endpoint.Port != session.Port)
                    {
                        if (!_tcpClient.Connected || endpoint.Address.ToString() != session.ServerAddress || endpoint.Port != session.Port)
                        {
                            if (_tcpClient != null)
                            {
                                _tcpClient.Dispose();
                                _tcpClient = null;
                            }
                        }

                        _resetEvent.Set();
                    }
                }
            }
        }

        ~TcpService()
        {
            Dispose(false);
        }
    }
}