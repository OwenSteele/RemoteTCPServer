using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RemoteTCPServer
{
    class Program
    {
        private static byte[] _buffer = new byte[1024];
        private static List<Socket> _clientSockets = new();
        private static Socket _serversocket = new (AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
        static void Main()
        {
            Console.Title = "OS SERVER";
            SetupServer();
            while (true) ;
        }

        private static void SetupServer()
        {
            Console.WriteLine("Setting up the server");
            _serversocket.Bind(new IPEndPoint(IPAddress.Any, 9999));
            _serversocket.Listen(10); //backlog = handled connections
            _serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            Console.WriteLine("Server set up");

        }
        private static void AcceptCallBack(IAsyncResult ar)
        {
            Socket socket = _serversocket.EndAccept(ar);
            _clientSockets.Add(socket);
            Console.WriteLine("Client Connected");
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBAck), socket);
            _serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null); //allows multiple connections
        }
        private static void RecieveCallBAck(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            int recieved = socket.EndReceive(ar);
            byte[] dataBuffer = new byte[recieved];
            Array.Copy(_buffer, dataBuffer, recieved);
            string text = Encoding.ASCII.GetString(dataBuffer);
            Console.WriteLine($"Msg Recieved: {text}");

            if (text.ToLower() == "get time") SendMessage(ref socket, DateTime.Now.ToLongDateString());
            else if (text.ToLower() == "hi nat") SendMessage(ref socket, "love you");
            else SendMessage(ref socket, "Invalid request");

        }
        private static void SendCallBack(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndSend(ar);
        }
        private static void SendMessage(ref Socket socket, string message)
        {
                byte[] data = Encoding.ASCII.GetBytes(message);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallBack), null);
                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBAck), socket);
        }
    }
}
