using System;
using System.Net;
using System.Net.Sockets;

namespace Megamind.Net.Sockets
{
    public class SocketClient
    {
        #region Data

        // client objects
        readonly byte[] _buffer;
        readonly Socket _socket;

        // A delegate type for hooking up change notifications.
        public delegate void ClientEventHandler(object sender);
        public delegate void DataEventHandler(object sender, SocketEventArgs e);

        // public events
        public event ClientEventHandler OnConnected;
        public event ClientEventHandler OnDisconnected;
        public event DataEventHandler OnAsyncConnectFailed;
        public event DataEventHandler OnDataSending;
        public event DataEventHandler OnDataReceived;
        public event DataEventHandler OnConnectionLost;

        #endregion

        #region Properties

        public int Port { get; set; }
        public string Host { get; set; }
        public bool IsConnected { get; private set; }

        public EndPoint LocalEndPoint
        {
            get { return _socket.LocalEndPoint; }
        }

        public EndPoint RemoteEndPoint
        {
            get { return _socket.RemoteEndPoint; }
        }

        public string EndPointString
        {
            get { return string.Format("{0} -> {1}", _socket.LocalEndPoint, _socket.RemoteEndPoint); }
        }

        #endregion

        #region ctor

        public SocketClient(string host, int port, int buffersize = 0xFFFF)
        {
            Host = host;
            Port = port;
            _buffer = new byte[buffersize]; //0xFFFF = 64k
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public virtual void Connect()
        {
            _socket.Connect(Host, Port);
            IsConnected = true;
            ClientConnected();
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback_Async, null);
        }

        public virtual void ConnectAsync()
        {
            _socket.BeginConnect(Host, Port, ConnectCallback_Async, null);
        }

        private void ConnectCallback_Async(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
                IsConnected = true;
                ClientConnected();
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback_Async, null);
            }
            catch (Exception ex)
            {
                AsyncConnectFailed(ex);
            }
        }

        public virtual void Disconnect()
        {
            IsConnected = false;
            ClientDisconnected();
            if (_socket == null) return;
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #endregion

        #region Read Write

        public int Send(byte[] data)
        {
            return _socket.Send(data);
        }

        public int SendData(byte[] data)
        {
            SocketDataSending(data);
            return _socket.Send(data);
        }

        private void ReceiveCallback_Async(IAsyncResult ar)
        {
            //disconnect from here, protect Object Disposed Exception
            if (!IsConnected) return;

            try
            {
                // exception on socket close or connection lost
                var bytesToRead = _socket.EndReceive(ar);
                if (bytesToRead == 0) //disconnect from remote
                {
                    ConnectionLost(new Exception("Disconnected from remote socket"));
                    Disconnect();
                    return;
                }
                var data = new byte[bytesToRead];
                Array.Copy(_buffer, data, bytesToRead);
                SocketDataReceived(data);
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback_Async, null);
            }
            catch (Exception ex) //conenction lost
            {
                ConnectionLost(ex);
                Disconnect();
            }
        }

        #endregion

        #region Event Handlers

        public virtual void AsyncConnectFailed(Exception ex)
        {
            OnAsyncConnectFailed?.Invoke(this, new SocketEventArgs(ex.Message));
        }

        public virtual void ClientConnected()
        {
            OnConnected?.Invoke(this);
        }

        public virtual void ClientDisconnected()
        {
            OnDisconnected?.Invoke(this);
        }

        public virtual void SocketDataSending(byte[] data)
        {
            OnDataSending?.Invoke(this, new SocketEventArgs(data));
        }

        public virtual void SocketDataReceived(byte[] data)
        {
            OnDataReceived?.Invoke(this, new SocketEventArgs(data));
        }

        public virtual void ConnectionLost(Exception ex)
        {
            OnConnectionLost?.Invoke(this, new SocketEventArgs(ex.Message));
        }

        #endregion
    }

}
