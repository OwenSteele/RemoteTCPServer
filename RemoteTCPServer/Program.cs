using System;
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
    internal class Server
    {
        internal static int maxBufferSize = 4096;
        private static byte[] _buffer = new byte[maxBufferSize];
        internal static List<ServerClient> clients = new();
        private static int backlogLimit = 10;
        private static Socket _serversocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static int serverPort = 0;
        private static string externalIP = null;


        private static bool sslEnabled = false;
        internal static X509Certificate serverCertificate = null;
        //the clients sever name must match with this
        private static string serverName = "OwensServer1";

        private static Dictionary<string, Func<ServerClient, string>> _openCommands = new()
        {
            { "help", ListAllCommands },
            { "serverinfo", ServerDetails },
            { "get time", OpenCommands.GetTime },
            { "login", OpenCommands.Login },
            { "logout", OpenCommands.Logout }
        };
        private static Dictionary<string, Func<ServerClient, string[], string>> _closedCommands = new()
        {
            { "<<login>>", ClosedCommands.LoginAttempt },
        };
        private static Dictionary<string, Func<ServerClient, string[], string>> _userCommands = new()
        {
            { "listclients", UserCommands.GetAllClients },
            { "clientinfo", UserCommands.GetClientInfo },
            { "kickclient", UserCommands.KickClient },
            { "serverrestart", UserCommands.RestartServer },
            { "sendfile", UserCommands.FileSentToServer },
            { "getfile", UserCommands.FileByClientRequest },
            { "setdir", UserCommands.SetServerDirPath }
        };
        private static string ListAllCommands(ServerClient client) => (String.Join("\n", _openCommands.Keys)) +
            ((client.CUser == null) ? "" : ("\n\n" + String.Join(" - USERS ONLY\n", _userCommands.Keys)));

        static void Main()
        {
            Console.Title = "OS SERVER";
            SetupServer(serverName);
            CreateUsers();

            Console.WriteLine();
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

            Console.WriteLine("\nObtaining External IP address");
            try
            {
                externalIP = new WebClient().DownloadString("http://icanhazip.com");
            }
            catch (Exception e)
            {
                externalIP = e.Message;
            }
        }
        private static string ServerDetails(ServerClient serverClient = null) =>
            "---Server details---" +
            $"\n     External IP: {externalIP}" +
            $"     Local IP: {GetLocalIPAddress()}" +
            $"\n     Port number: {serverPort}" +
            $"\n{(sslEnabled ? $"     SSL server name : '{serverName}'" : "")}";

        private static void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                Socket socket = _serversocket.EndAccept(ar);
                clients.Add(new ServerClient(socket));
                Console.WriteLine("Client Connected");
                if (sslEnabled) SSLCertification.CertifyClient(socket, serverCertificate);

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
                ServerClient currentClient = clients.Find(s => s.CSocket == socket);
                try
                {
                    int currentClientPos = clients.FindIndex(s => s.CSocket == socket) + 1;

                    int recieved = socket.EndReceive(ar);
                    byte[] dataBuffer = new byte[recieved];
                    Array.Copy(_buffer, dataBuffer, recieved);
                    string text = Encoding.ASCII.GetString(dataBuffer);

                    if (text.Contains("###`CLIENTINFO`###"))
                    {
                        string[] temp = text.Split(' ');
                        clients.Find(s => s.CSocket == socket).MachineName = temp[2];
                        clients.Find(s => s.CSocket == socket).IP = IPAddress.Parse(temp[3]);
                    }

                    if (text.Contains("<<") && text.Contains(">>"))
                        OpenCommands.currentTag = text.Substring(text.IndexOf("<<"), (text.IndexOf(">>") - text.IndexOf("<<")) + 2);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"[{currentClientPos}]:");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"{currentClient.MachineName}|{currentClient.IP}" +
                        $"{((currentClient.CUser != null) ? $"('{currentClient.CUser}', {currentClient.CUser.GetSecurity()})" : "")}");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"      $ '{text}'");

                    string message = ProcessRequest(text, socket);

                    if (text.Contains("###`CLIENTINFO`###") && !clients.Find(s => s.CSocket == socket).DetailsSent)
                    {
                        message = "Owen's TCP server 2020.\n\n" + ServerDetails();
                        clients.Find(s => s.CSocket == socket).DetailsSent = true;
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"[{currentClientPos}]:");
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($" >> '{RemoveNewLines(message)}'");
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
                    RemoveClient(currentClient,ex.Message);
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            catch (Exception ex)
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
        private static string ProcessRequest(string req, Socket socket)
        {
            ServerClient serverClient = GetClient(socket);
            if (req.Contains("<<") && req.Contains(">>"))
            {
                string functionTag = null;
                functionTag = req.Substring(req.IndexOf("<<"), (req.IndexOf(">>") - req.IndexOf("<<")) + 2);
                req = req.Substring(req.IndexOf(">>") + 2, req.Length - (req.IndexOf(">>") + 2));
                if (_closedCommands.TryGetValue(functionTag, out Func<ServerClient, string[], string> calledFunction))
                    return calledFunction(serverClient, req.Split('|'));
            }
            else
            {
                if (_openCommands.TryGetValue(req, out Func<ServerClient, string> calledFunction)) return calledFunction(serverClient);

                if (serverClient.CUser == null) return "Invalid request. Type 'help' for commands";
                if (serverClient.CUser.ID == null) return "Invalid request. Type 'help' for commands";

                if (req.StartsWith(clients.Find(c => c.CSocket == socket).CUser.ID))
                    {
                        string[] reqs = req.Split(' ');
                        if (reqs.Length >= 2)
                        if (_userCommands.TryGetValue(reqs[1],
                            out Func<ServerClient, string[], string> calledUserFunction)) return calledUserFunction(serverClient, reqs);
                    }
                    else return $"Invalid request. Type 'help' for commands.\n" +
                            $"NOTE: For user commands, include user ID: '{serverClient.CUser.ID} {req}'.";
            }
            return "Invalid request. Type 'help' for commands";
        }
        private static string RemoveNewLines(string text) => text.Replace("\n", "[nLn]");
        private static void CreateUsers() => UserFactory.Create();
        private static ServerClient GetClient(Socket socket) => clients.Find(c => c.CSocket == socket);

        internal static bool Restart()
        {
            try
            {
                try
                {
                    _serversocket.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
        private static void RemoveClient(ServerClient client, string message = null)
        {
            if (client != null)
            {
                int pos = clients.FindIndex(s => s.CSocket == client.CSocket);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"[{pos + 1}]:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($" {message}");
                if (pos > -1) clients.RemoveAt(clients.FindIndex(s => s.MachineName == client.MachineName));
            }
            Console.WriteLine($"Client [{client.MachineName} | {client.MachineName}] removed from list");
        }
    }
}
