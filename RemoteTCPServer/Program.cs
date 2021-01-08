using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using RemoteTCPServer.Commands;
using System.Collections.Generic;

namespace RemoteTCPServer
{
    internal class Server
    {
        static void Main()
        {
            Console.Title = "OS SERVER";
            SetupServer(SP.ServerName);

            CreateUsers();
            CommandFactory.CreateCommands();

            Console.WriteLine();
            Console.Write("please wait...");
            Console.WriteLine($"\r {new OpenCommands().ServerDetails()}");
            Console.WriteLine("---Ready for SP.Clients---\n");

            while (true) ;
        }        

        private static void SetupServer(string certificate)
        {
            Console.Write("Set Server Port Number: ");
            while (true)
            {
                string inputPort = Console.ReadLine();
                if (Int32.TryParse(inputPort, out SP.ServerPort))
                {
                    if (SP.ServerPort < 0 || SP.ServerPort > 65535) Console.Write("\r Value out of legal port bound (0 to 65535), try again: ");
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
                    SP.ServerCertificate = X509Certificate.CreateFromCertFile(certificate);
                    SP.SslEnabled = true;
                    break;
                }
                else if (key == ConsoleKey.N) break;
                else Console.Write("\rInvalid input, try again [Y/N]: ");
            }
            Console.WriteLine("\n\nSetting up the server");
            SP.Serversocket.Bind(new IPEndPoint(IPAddress.Any, SP.ServerPort));
            SP.Serversocket.Listen(SP.BacklogLimit); //backlog = handled connections
            
            Console.WriteLine("\nObtaining External IP address");
            try
            {
                SP.ExternalIP = new WebClient().DownloadString("http://icanhazip.com");
            }
            catch (Exception)
            {
                SP.ExternalIP = "IP retrieval timed out.";
            }

            SP.Serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            Console.WriteLine("Server set up");

            
        }
        
        private static void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                Socket socket = SP.Serversocket.EndAccept(ar);
                SP.Clients.Add(new ServerClient(socket));
                Console.WriteLine("Client Connected");
                if (SP.SslEnabled) SSLCertification.CertifyClient(socket, SP.ServerCertificate);

                socket.BeginReceive(SP.Buffer, 0, SP.Buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBAck), socket);
                SP.Serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null); //allows multiple connections
            }
            catch (SocketException ex)
            {
                ExceptionHandling.Print(ex);
            }
            catch (ObjectDisposedException ex)
            {
                ExceptionHandling.Print(ex);
            }
        }
        private static void RecieveCallBAck(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                ServerClient currentClient = SP.Clients.Find(s => s.CSocket == socket);
                try
                {
                    int currentClientPos = SP.Clients.FindIndex(s => s.CSocket == socket) + 1;

                    int recieved = socket.EndReceive(ar);
                    byte[] dataBuffer = new byte[recieved];
                    Array.Copy(SP.Buffer, dataBuffer, recieved);
                    string text = Encoding.ASCII.GetString(dataBuffer);

                    if (text.Contains("###`CLIENTINFO`###"))
                    {
                        string[] temp = text.Split(' ');
                        SP.Clients.Find(s => s.CSocket == socket).MachineName = temp[2];
                        SP.Clients.Find(s => s.CSocket == socket).IP = IPAddress.Parse(temp[3]);
                    }

                    if (text.Contains("<<") && text.Contains(">>"))
                        currentClient.OpenCommands.CurrentTag = text.Substring(text.IndexOf("<<"), (text.IndexOf(">>") - text.IndexOf("<<")) + 2);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"[{currentClientPos}]:");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"{currentClient.MachineName}|{currentClient.IP}:{currentClient.CSocket.Handle}" +
                        $"{((currentClient.CUser != null) ? $"('{currentClient.CUser.ID}', {currentClient.CUser.GetAccess()})" : "")}");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"      $ '{text}'");

                    string message = ProcessRequest(text, socket);

                    if (text.Contains("###`CLIENTINFO`###") && !SP.Clients.Find(s => s.CSocket == socket).DetailsSent)
                    {
                        message = "Owen's TCP server 2020.\n\n" + currentClient.OpenCommands.ServerDetails() + "\n\n Type 'help' to see all commands";
                        SP.Clients.Find(s => s.CSocket == socket).DetailsSent = true;
                    }

                    PrepareSend(currentClientPos, message, socket);
                }
                catch (SocketException ex)
                {
                    RemoveClient(currentClient,ex);
                }
                catch (ObjectDisposedException ex)
                {
                    ExceptionHandling.Print(ex);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandling.Print(ex);
            }
        }
        public static void PrepareSend(int pos, string message, Socket socket)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{pos}]:");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($" >> '{RemoveNewLines(message)}'");
            Console.ForegroundColor = ConsoleColor.Gray;

            if (!String.IsNullOrEmpty(message))
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallBack), null);
                socket.BeginReceive(SP.Buffer, 0, SP.Buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBAck), socket);
                message = null;
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

        private static string ProcessRequest(string req, Socket socket)
        {
            string invalidRequest = "Invalid request. Type 'help' for commands";
            //Determine the destination of the request

            ServerClient serverClient = GetClient(socket);

            if (serverClient == null) throw new ArgumentNullException();

            //closed commands
            string closedResponse = serverClient.ClosedCommands.Call(req);
            if (closedResponse != null) return closedResponse;

            //user ID check
            string clientUserID = null;
            if (serverClient.CUser != null) clientUserID = SP.Clients.Find(c => c.CSocket == socket).CUser.ID;
            
            //user commands
            if (clientUserID != null && req.StartsWith(clientUserID))
            {
                string userResponse = serverClient.UserCommands.Call(req);
                if (userResponse != null) return userResponse;
            }
            else invalidRequest += "\nREMINDER: user commands must start with your user ID";
                  
            //open commands
            string openResponse = serverClient.OpenCommands.Call(req);
            if (openResponse != null) return openResponse;            

            //no command found
            return invalidRequest;
        }

        private static string RemoveNewLines(string text) => text.Replace("\n", "[nLn]");
        private static void CreateUsers() => UserFactory.Create();
        private static ServerClient GetClient(Socket socket) => SP.Clients.Find(c => c.CSocket == socket);

        internal static bool Restart()
        {
            try
            {
                try
                {
                    SP.Serversocket.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    ExceptionHandling.Print(ex);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandling.Print(ex);
            }
            return false;
        }
        private static void RemoveClient(ServerClient client, Exception ex)
        {
            ExceptionHandling.Print(ex);
            if (client != null)
            {
                int pos = SP.Clients.FindIndex(s => s.CSocket == client.CSocket);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"\n[{pos + 1}]:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($" {ex.Message}");
                if (pos > -1) SP.Clients.RemoveAt(pos);
            }
            if (client != null) Console.WriteLine($"     Client [{client.MachineName} | {client.IP}:{client.CSocket.Handle}] removed from list");
        }
    }
}
