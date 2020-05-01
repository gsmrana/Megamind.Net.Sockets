using System;

namespace Megamind.Net.Sockets
{
    public class SocketEventArgs : EventArgs
    {
        public byte[] Data { get; private set; }
        public string Info { get; private set; }

        public SocketEventArgs(byte[] data)
        {
            Data = data;
        }

        public SocketEventArgs(string info)
        {
            Info = info;
        }
    }
}
