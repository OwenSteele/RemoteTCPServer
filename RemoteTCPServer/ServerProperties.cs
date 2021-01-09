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
        public static int MaxBufferSize { get; set; } = 4096; // maximum client-server packet size
        public static byte[] Buffer = new byte[MaxBufferSize];
        public static List<ServerClient> Clients = new(); // global list of the connected clients
        public static int BacklogLimit = 100; // != client count
        public static Socket Serversocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // server/host socket set up
        public static int ServerPort = 0;
        public static string ExternalIP = null; //for non-remote limited connections

        public static string[] SqlInfo = new string[4]; // servername, dbname, user id, password

        public static bool SslEnabled = false;
        public static X509Certificate ServerCertificate = null; //set to certificate to use SSL
        public static string ServerName = "OwensServer1"; //the clients sever name must match with this

        public static IPAddress GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork) return ip;
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
