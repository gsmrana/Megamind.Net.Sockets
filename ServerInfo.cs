using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Megamind.Net.Sockets
{
    public static class ServerInfo
    {
        #region Properties

        public static IEnumerable<IPAddress> GetLocalIPs
        {
            get
            {
                var hostentry = Dns.GetHostEntry(Dns.GetHostName());
                return hostentry.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork);
            }
        }

        public static IEnumerable<TcpConnectionInformation> GetTcpEndpoints
        {
            get
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpconns = ipGlobalProperties.GetActiveTcpConnections();
                return tcpconns.Where(x => x.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork); // remove ipv6 entry
            }
        }

        public static IEnumerable<IPEndPoint> GetTcpListeners
        {
            get
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var ipEnpoint = ipGlobalProperties.GetActiveTcpListeners();
                return ipEnpoint.Where(x => x.AddressFamily == AddressFamily.InterNetwork); // remove ipv6 entry
            }
        }

        public static IEnumerable<IPEndPoint> GetUdpListeners
        {
            get
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var ipEnpoint = ipGlobalProperties.GetActiveUdpListeners();
                return ipEnpoint.Where(x => x.AddressFamily == AddressFamily.InterNetwork); // remove ipv6 entry
            }
        }

        #endregion

        #region Public Methods

        public static bool IsTcpListening(int port)
        {
            return GetTcpListeners.Any(x => x.Port == port);
        }

        public static IEnumerable<IPAddress> ResolveDns(string host)
        {
            var hostentry = Dns.GetHostEntry(host);
            return hostentry.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork);
        }

        public static string ResolveHostname(IPAddress ipaddress)
        {
            var hostentry = Dns.GetHostEntry(ipaddress);
            return hostentry.HostName;
        }

        public static IEnumerable<PingReply> GetTraceRoute(string host, int timeout = 3000, int maxTTL = 30)
        {
            const int bufferSize = 32;
            var buffer = new byte[bufferSize];
            new Random().NextBytes(buffer);

            using (var pinger = new Ping())
            {
                for (int ttl = 1; ttl <= maxTTL; ttl++)
                {
                    var options = new PingOptions(ttl, true);
                    var reply = pinger.Send(host, timeout, buffer, options);
                    yield return reply;

                    // we're done searching or there has been an error
                    if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
                        break;
                }
            }
        }

        public static DateTime GetDatetimeFromNTP(string host, int port = 123)
        {
            const byte ntpDateTimeIndex = 40;
            var ntpData = new byte[48];   // NTP message size - 16 bytes of the digest (RFC 2030)
            ntpData[0] = 0x1B;  //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

            // Request data from server
            var ntpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ntpSocket.Connect(host, port);
            ntpSocket.Send(ntpData);
            ntpSocket.ReceiveTimeout = 3000;
            ntpSocket.Receive(ntpData);
            ntpSocket.Close();

            // Calculate the DateTime from milliseconds
            ulong intPart = SwapEndianness(BitConverter.ToUInt32(ntpData, ntpDateTimeIndex));
            ulong fractPart = SwapEndianness(BitConverter.ToUInt32(ntpData, ntpDateTimeIndex + 4));
            ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            var ntpDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds).ToLocalTime();

            return ntpDateTime;
        }

        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }

        #endregion
    }
}
