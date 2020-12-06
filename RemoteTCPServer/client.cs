using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public class ServerClient
    {
        public Socket CSocket;
        public User CUser;

        public ServerClient(Socket socket)
        {
            CSocket = socket;
        }
    }
}
