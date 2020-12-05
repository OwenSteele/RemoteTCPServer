﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;

namespace RemoteTCPServer
{
    sealed class Server
    {
        private static byte[] _buffer = new byte[1024];
        private static List<Socket> _clientSockets = new();
        private static int backlogLimit = 10;
        private static Socket _serversocket = new (AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
        private static int serverPort = 0;

        private static bool sslEnabled = false;
        internal static X509Certificate serverCertificate = null;
        //the clients sever name must match with this
        private static string serverName = "OwensServer1";

        
        

        private static Dictionary<string, Func<string>> _openCommands = new()
        {
            { "###`INITclientINFOrequest`###", ServerDetails },
            { "get time", OpenCommands.GetTime },
            { "login", OpenCommands.Login }
        };

        static void Main()
        {     
            Console.Title = "OS SERVER";
            SetupServer(serverName);

            Console.Write("please wait...");
            Console.WriteLine($"\r {ServerDetails()}");
            Console.WriteLine("---Ready for clients---\n");
            while (true) ;
        }

        private static void SetupServer(string certificate)
        {
            Console.Write("Set Server Port Number: ");
            while (true)
            {
                string inputPort = Console.ReadLine();
                if (Int32.TryParse(inputPort, out serverPort))
                {
                    if (serverPort < 0 || serverPort > 65535) Console.Write("\r Value out of legal port bound (0 to 65535), try again: ");
                    else break;
                }
                else Console.Write("\r Invalid input, try again: ");
            }
            Console.Write("Set up SSL? [Y/N]: ");
            while (true)
            {
                ConsoleKey key = Console.ReadKey().Key;
                if (key == ConsoleKey.Y)
                {
                    serverCertificate = X509Certificate.CreateFromCertFile(certificate);
                    sslEnabled = true;
                    break;
                }
                else if (key == ConsoleKey.N) break;
                else Console.Write("\rInvalid input, try again [Y/N]: ");
            }
            Console.WriteLine("\n\nSetting up the server");
            _serversocket.Bind(new IPEndPoint(IPAddress.Any, serverPort));
            _serversocket.Listen(backlogLimit); //backlog = handled connections
            _serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            Console.WriteLine("Server set up");

        }
        private static string ServerDetails() => "---Server details---"+
            $"\n     External IP: {new WebClient().DownloadString("http://icanhazip.com")}"+
            $"     Local IP: {GetLocalIPAddress()}"+
            $"\n     Port number: {serverPort}"+
            $"\n{(sslEnabled ? $"     SSL server name : '{serverName}'" : "")}\n";
        
        private static void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                Socket socket = _serversocket.EndAccept(ar);
                _clientSockets.Add(socket);
                Console.WriteLine("Client Connected");
                if(sslEnabled) SSLCertification.CertifyClient(socket, serverCertificate);

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBAck), socket);
                _serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null); //allows multiple connections
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }        
        private static void RecieveCallBAck(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                int recieved = socket.EndReceive(ar);
                byte[] dataBuffer = new byte[recieved];
                Array.Copy(_buffer, dataBuffer, recieved);
                string text = Encoding.ASCII.GetString(dataBuffer);

                if (text.Contains("<<") && text.Contains(">>"))
                {
                    OpenCommands.currentTag = text.Substring(text.IndexOf("<<"), (text.IndexOf(">>")-text.IndexOf("<<"))+2);
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"Msg Recieved: {text}");

                string message = ProcessRequest(text);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"   [Reply sent: {RemoveNewLines(message)}]");
                Console.ForegroundColor = ConsoleColor.Gray;

                if (!String.IsNullOrEmpty(message))
                {
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallBack), null);
                    socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBAck), socket);
                    message = null;
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static void SendCallBack(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                if (socket != null) socket.EndSend(ar);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static IPAddress GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork) return ip;
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        private static string ProcessRequest(string req)
        {
            if(!_openCommands.TryGetValue(req, out Func<string> calledFunction)) return "Invalid request";
            return calledFunction(); 
        }
        private static string RemoveNewLines(string text) => text.Replace("\n","[nLn]");
    }
}
