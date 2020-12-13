using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace RemoteTCPServer
{
    //Abbreviated to SP to reduce verbosity
    public static class SP
    {
        public static int MaxBufferSize { get; set; } = 4096;
        public static byte[] Buffer = new byte[MaxBufferSize];
        public static List<ServerClient> Clients = new();
        public static int BacklogLimit = 10;
        public static Socket Serversocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static int ServerPort = 0;
        public static string ExternalIP = null;

        public static bool SslEnabled = false;
        public static X509Certificate ServerCertificate = null;
        //the clients sever name must match with this
        public static string ServerName = "OwensServer1";

        public static IPAddress GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork) return ip;
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
