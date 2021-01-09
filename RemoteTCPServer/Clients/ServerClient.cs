using RemoteTCPServer.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using SQLNet;

namespace RemoteTCPServer
{
    public class ServerClient
    {
        public Socket CSocket { get; init; }
        public User CUser { get; set; }
        public string MachineName { get; set; }
        public IPAddress IP { get; set; }
        public bool DetailsSent { get; set; } = false;

        public SqlAPI SqlClient { get; set; }

        public UserCommands UserCommands { get; } = new();
        public OpenCommands OpenCommands { get; } = new();
        public ClosedCommands ClosedCommands { get; } = new();
        public SqlCommands SqlCommands { get; } = new();

        public ServerClient(Socket socket)
        {
            CSocket = socket;
        }
    }
}
