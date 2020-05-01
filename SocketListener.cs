using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Megamind.Net.Sockets
{
    public class SocketListener
    {
        #region Data

        // socket listener object      
        readonly Socket _listener;

        // delegate type for hooking up change notifications.
        public delegate void ClientEventHandler(Socket socket);
        public event ClientEventHandler OnClientConnected;

        #endregion

        #region Properties

        public bool IsListening { get; private set; }

        public IPEndPoint LocalEndPoint { get; private set; }

        #endregion

        #region ctor

        public SocketListener(int port)
            : this(IPAddress.Any, port)
        {

        }

        public SocketListener(IPAddress bindIp, int port)
        {
            LocalEndPoint = new IPEndPoint(bindIp, port);
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start()
        {
            _listener.Bind(LocalEndPoint);
            _listener.Listen(100);
            IsListening = true;
            _listener.BeginAccept(AcceptCallback_Handler, null);
        }

        public void Stop()
        {
            IsListening = false;
            if (_listener == null) return;
            _listener.Close();
            _listener.Dispose();
        }

        private void AcceptCallback_Handler(IAsyncResult ar)
        {
            // listener stopped, protect from Object Disposed Exception
            if (!IsListening) return;

            try
            {
                var client = _listener.EndAccept(ar);
                ClientConnected(client);
                _listener.BeginAccept(AcceptCallback_Handler, null);
            }
            catch (Exception ex) //something wrong
            {
                Debug.WriteLine("Listener Exception: " + ex.Message);
            }
        }

        #endregion

        #region Event Handlers

        public virtual void ClientConnected(Socket client)
        {
            OnClientConnected?.Invoke(client);
        }

        #endregion

    }
}
