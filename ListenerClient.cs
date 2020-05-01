using System;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace Megamind.Net.Sockets
{
    public class ListenerClient
    {
        #region Data

        // client properties
        readonly Timer _timeout;
        readonly Socket _socket;
        readonly byte[] _buffer = new byte[0xFFFF]; //64K byte

        // delegate type for hooking up change notifications.
        public delegate void ClientEventHandler(ListenerClient sender);
        public delegate void DataEventHandler(ListenerClient sender, SocketEventArgs e);

        // public events         
        public event DataEventHandler OnDataSending;
        public event DataEventHandler OnDataReceived;
        public event DataEventHandler OnConnectionLost;
        public event ClientEventHandler OnClientIdleTimeout;
        public event ClientEventHandler OnClientDisconnected;

        #endregion

        #region Properties

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
            get { return string.Format("{0} <- {1}", _socket.LocalEndPoint, _socket.RemoteEndPoint); }
        }

        #endregion

        #region ctor

        public ListenerClient(Socket client)
        {
            _timeout = new Timer();
            _timeout.AutoReset = false; //timeout event trigger only once
            _timeout.Elapsed += IdleClientTimeoutHandler;

            _socket = client;
            IsConnected = true;
        }

        public void BeginReceive()
        {
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReadCallback_Async, null);
        }

        public virtual void Disconnect()
        {
            if (_timeout != null)
            {
                _timeout.Stop();
                _timeout.Close();
            }
            IsConnected = false;
            ClientDisconnected();
            if (_socket != null)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
        }

        #endregion

        #region Idle Connection Timeout

        public void SetIdleClientTimeout(int second)
        {
            _timeout.Interval = second * 1000;
            _timeout.Start();
        }

        private void ResetIdleClientTimeout()
        {
            _timeout.Stop();
            _timeout.Start();
        }

        private void IdleClientTimeoutHandler(object sender, ElapsedEventArgs e)
        {
            ClientIdleTimeout();
            Disconnect();
        }

        #endregion

        #region Read Write
        public int Send(byte[] data)
        {
            //without event triggering
            return _socket.Send(data);
        }

        public int SendData(byte[] data)
        {
            SocketDataSending(data);
            return _socket.Send(data);
        }

        private void ReadCallback_Async(IAsyncResult ar)
        {
            //disconnect from here, protect from Object Disposed Exception
            if (!IsConnected) return;

            //reset the idle timeout timer
            if (_timeout.Enabled) ResetIdleClientTimeout();

            try
            {
                // exception on socket close or connection lost
                var bytesRead = _socket.EndReceive(ar);
                if (bytesRead == 0) //disconnect from remote
                {
                    ConnectionLost(new Exception("Disconnected from remote socket"));
                    Disconnect();
                    return;
                }
                var buffer = new byte[bytesRead];
                Array.Copy(_buffer, buffer, bytesRead);
                SocketDataReceived(buffer);
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReadCallback_Async, null);
            }
            catch (Exception ex) //conenction lost
            {
                ConnectionLost(ex);
                Disconnect();
            }
        }

        #endregion

        #region Event Handlers

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

        public virtual void ClientIdleTimeout()
        {
            OnClientIdleTimeout?.Invoke(this);
        }

        public virtual void ClientDisconnected()
        {
            OnClientDisconnected?.Invoke(this);
        }

        #endregion
    }

}
