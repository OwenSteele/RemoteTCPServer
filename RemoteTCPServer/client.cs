using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public class ServerClient
    {
        public Socket CSocket { get; init; }
        public User CUser { get; set; }
        public string MachineName { get; set; }
        public IPAddress IP { get; set; }

        public bool DetailsSent { get; set; } = false;

        public ServerClient(Socket socket) => CSocket = socket;
    }
}
