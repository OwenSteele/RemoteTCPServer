﻿using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace RemoteTCPServer
{
    public static class Commands
    {
            public static Dictionary<string, Func<ServerClient, string>> baseCommands = new()
            {
                { "help", ListAllCommands },
                { "serverinfo", ServerDetails },
                { "get time", GetTime },
                { "login", Login },
                { "logout", Logout }
            };
            private static string ListAllCommands(ServerClient client) => "\n-----All available commands-----\n" + (String.Join("\n", baseCommands.Keys)) +
                ((client.CUser == null) ? "" : ((client.CUser.ID == null) ? "" :($"\n\n --USER ONLY COMMANDS--\n  Must be preceded by the user ID\ne.g. '{client.CUser.ID} <command>'\n" + String.Join(" - USERS ONLY\n", Users.commands.Keys) + " - USERS ONLY\n")));
            public static string ServerDetails(ServerClient serverClient = null) =>
                "---Server details---" +
                $"\n     External IP: {SP.ExternalIP}" +
                $"     Local IP: {SP.GetLocalIPAddress()}" +
                $"\n     Port number: {SP.ServerPort}" +
                $"\n{(SP.SslEnabled ? $"     SSL server name : '{SP.ServerName}'" : "")}";

            public static string currentTag = null;
            private static string GetTime(ServerClient serverClient) => DateTime.Now.ToLongTimeString();
            public static string Login(ServerClient serverClient)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Client request to log on. ");

                if (serverClient.CUser != null)
                    if (serverClient.CUser.ID != null) return "You are already logged in.\n" +
                        $"To switch accounts enter 'logout {serverClient.CUser.ID}'";
                Console.ForegroundColor = ConsoleColor.Gray;
                return "<NoH>User: #/C.RL/#Password: #/C.RL/#<<login.MR>>";
            }
            public static string Logout(ServerClient serverClient)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Client request to log out. ");

                if (serverClient.CUser == null) return "You are not logged in.\n" +
                        "To log in enter 'login'";
                Console.ForegroundColor = ConsoleColor.Gray;
                serverClient.CUser = new User(null, "");
                return "You have logged out.";
            }        
        public static class Closed
        {
            public static Dictionary<string, Func<ServerClient, string[], string>> commands = new()
            {
                { "<<login>>", LoginAttempt },
            };

            public static string LoginAttempt(ServerClient serverClient, string[] details)
            {
                User user = UserFactory.GetUser(details[0]);
                if (user.ID == details[0])
                    if (user.CheckPassword(details[1]))
                    {
                        serverClient.CUser = user;
                        return "Success, you are now logged in ";
                    }
                return "Error, could not log in";
            }
        }
        public static class Users
        {
            public static Dictionary<string, Func<ServerClient, string[], string>> commands = new()
            {
                { "listclients", GetAllClients },
                { "clientinfo", GetClientInfo },
                { "kickclient", KickClient },
                { "serverrestart", RestartServer },
                { "sendfile", FileSentToServer },
                { "getfile", FileByClientRequest },
                { "setdir", SetServerDirPath },
                { "message", Messaging}
            };

            public static string directoryPath = null;
            public static string GetAllClients(ServerClient serverClient, string[] args = null)
            {
                if (!serverClient.CUser.AllowedAccess(0)) return "Access Level not high enough.";

                string clientList = null;
                int clientPos = 1;
                foreach (ServerClient client in SP.Clients)
                {
                    clientList += GetClientInfo(client);
                    clientPos++;
                }
                return clientList;
            }
            public static string KickClient(ServerClient serverClient, string[] strPos)
            {
                if (!serverClient.CUser.AllowedAccess(0)) return "Access Level not high enough.";

                if (!Int32.TryParse(strPos[2], out int pos)) return $"Must input a value between 1 and {SP.Clients.Count}";
                if (SP.Clients[pos - 1].CUser.ID == serverClient.CUser.ID) return "You cannot remove this client, disconnect instead.";
                if (pos > 0 && pos <= SP.Clients.Count)
                {
                    SP.Clients[pos - 1].CSocket.Disconnect(true);
                    SP.Clients.RemoveAt(pos - 1);
                    return "Client removed - NOTE: client positions will now have changed";
                }
                return "Client position not found.";
            }
            public static string RestartServer(ServerClient serverClient, string[] confirmed)
            {
                if (serverClient.CUser.AllowedAccess(0))
                {
                    if (confirmed[2] == "force")
                    {
                        if (Server.Restart()) return "Server Restarted.";
                        else return "Error, server could not be restarted.";
                    }
                    return "Restart requires 'force', to confrim call.";
                }
                return "Access Level not high enough.";
            }
            public static string GetClientInfo(ServerClient serverClient, string[] args = null)
            {
                string id = "Not logged in";
                if (serverClient.CUser != null) id = serverClient.CUser.ID;

                return $"\n\nID: {id}\n" +
                    $"IP address: {serverClient.IP}\n" +
                    $"Machine name: {serverClient.MachineName}\n" +
                    $"Connection order: {SP.Clients.IndexOf(serverClient)}\n" +
                    $"Handle: {serverClient.CSocket.Handle}\n";
            }
            public static string FileSentToServer(ServerClient serverClient, string[] fileData)
            {
                string dirMsg = $"{((directoryPath == null) ? "No directory set, must be created by admin first" : "")}";
                string help = "HELP: Send a file of specified type to the server to be saved permanently." + dirMsg +
                    $"{((dirMsg == null) ? "\n" : "")}  Syntax: '<userID> sendfile <full file path>'. The path must be a location on this machine.";

                if (!serverClient.CUser.AllowedAccess(1)) return "Access Level not high enough.";
                if (fileData.Length < 4) return help;
                if (String.IsNullOrWhiteSpace(directoryPath)) return $"Error: {dirMsg}";

                string fileName = fileData[2];
                string fileContent = null;
                for (int i = 3; i < fileData.Length; i++) fileContent += fileData[i] + " ";

                byte[] fileBytes = Encoding.ASCII.GetBytes(fileContent);

                try
                {
                    File.WriteAllBytes(directoryPath + fileName, fileBytes);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return "Could not save file to server";
                }
                return "File saved to server successfully";
            }
            public static string FileByClientRequest(ServerClient serverClient, string[] fileData)
            {
                string dirMsg = $"{((directoryPath == null) ? "No directory set, must be created by admin first" : "")}";
                string help = "HELP: Recieve a file of specified type from the server to be saved permanently on this machine." + dirMsg +
                    $"{((dirMsg == null) ? "\n" : "")}  Syntax: '<userID> sendfile <file name>'. File name only, '*.fileType' must be included.";

                if (!serverClient.CUser.AllowedAccess(1)) return "Access Level not high enough.";
                if (fileData.Length < 4) return help;
                if (String.IsNullOrWhiteSpace(directoryPath)) return $"Error: {dirMsg}";
                if (!File.Exists(directoryPath + fileData[2])) return "Invalid file name, not found in server directory";
                if (File.ReadAllBytes(directoryPath + fileData[2]).Length > SP.MaxBufferSize) return "File too large to send.";

                if (!fileData[3].EndsWith('/')) fileData[3] += '/';
                return $"<<fileTransfer>> {fileData[3] + fileData[2]} {Encoding.ASCII.GetString(File.ReadAllBytes(directoryPath + fileData[2]))}";
            }
            public static string SetServerDirPath(ServerClient serverClient, string[] fileData)
            {
                string help = "HELP: set or change the server directory path with this command.\n" +
                        $"{((directoryPath == null) ? "No directory set, to send or recieve files a directory needs to be set." : $"Current directory: '{directoryPath}'")}";

                if (!serverClient.CUser.AllowedAccess(0)) return "Access Level not high enough.";
                if (fileData.Length < 3) return help;
                if (String.IsNullOrWhiteSpace(fileData[2])) return help;
                if (Directory.Exists(fileData[2]))
                {

                    directoryPath = fileData[2].Replace('\\', '/');
                    if (directoryPath.Substring(directoryPath.Length - 1, 1) != "/") directoryPath += "/";
                    return $"Directory successfully changed to : {fileData[2]}";
                }
                else return "Invalid path, directory not found";

            }
            public static string Messaging(ServerClient serverClient, string[] fileData)
            {
                if(serverClient.CUser.SecurityState.Messaging == 0) return $"Your messaging security state is set to '{serverClient.CUser.SecurityState.GetState(0)}'\nIt must be changed before using this function.";

                string help = "Message other active clients\n" +
                    "Clients must also be logged in to recieve messages and not have their security set to 'private'\n\n" +
                    $"Syntax: {fileData[0]} message <ip>:<socketHandle> <your message>\n" +
                    $"   e.g. {fileData[0]} message {serverClient.IP}:{serverClient.CSocket.Handle.ToString()} Hello there!\n\n" +
                    $"To list all contactable clients:\n" +
                    $"Syntax: {fileData[0]} message list";                

                (List<ServerClient>, string) openClients = OpenClientListRequest(serverClient);

                if (fileData.Length == 3) if(fileData[2] == "list") return openClients.Item2;
                if (fileData.Length < 4) return help;                                
                if (!IPAddress.TryParse(fileData[2].Split(':')[0], out IPAddress targetIP)) return "Invalid syntax for IPv4 Address";
                 
                ServerClient target = openClients.Item1.Find(
                    c => c.CSocket.Handle.ToString() == fileData[2].Split(':')[1] &&
                    c.IP.ToString() == targetIP.ToString());
                if (target == null) return $"Client not found.\n Connected clients (logged in):\n {openClients.Item2}";

                if (SameMachine(target,serverClient))return "You cannot message yourself.";

                string message = null;
                for (int i = 3; i < fileData.Length; i++) message += $"{fileData[i]} ";

                string allData = $"<<client[{serverClient.IP}:{serverClient.CSocket.Handle} ({serverClient.CUser.ID})].message{MessageClient(target.CUser.SecurityState.Messaging)}>> " +
                    $"{message.Trim()}";

                Server.PrepareSend(SP.Clients.IndexOf(serverClient), allData, target.CSocket);
                return "Message sent";
            }
            private static (List<ServerClient>,string) OpenClientListRequest(ServerClient currentClient,bool messaging = true)
            {
                string ipList = "list:\n";
                List<ServerClient> list = new();
                foreach (ServerClient client in SP.Clients)
                    if (client.CUser != null)
                    {
                        if ((messaging && client.CUser.SecurityState.Messaging != 0) || (!messaging && client.CUser.SecurityState.FileTransfer != 0))
                        {
                            list.Add(client);
                            ipList += $"{client.IP.ToString()}:{client.CSocket.Handle} {(SameMachine(client, currentClient) ? "<- your machine":"")}\n";
                        }
                    }
                return (list,ipList);
            }
            private static bool SameMachine(ServerClient one, ServerClient two)
            {
                if ($"{one.IP}:{one.CSocket.Handle}" == $"{two.IP}:{two.CSocket.Handle}")
                    return true;
                return false;
            }
            private static string MessageClient(int promptRequired)
            {
                if (promptRequired == 1) return "|request";
                else if (promptRequired != 2) throw new ArgumentOutOfRangeException();
                return "";
            }
        }
    }
}

