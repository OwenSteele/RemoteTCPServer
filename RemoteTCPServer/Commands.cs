using System;
using System.Linq;
using System.IO;

namespace RemoteTCPServer
{
    public static class OpenCommands
    {
        public static string currentTag = null;
        public static string GetTime(ServerClient serverClient) => DateTime.Now.ToLongTimeString();
        public static string Login(ServerClient serverClient)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" Client request to log on. ");

            if (serverClient.CUser != null)
                if (serverClient.CUser.ID != null) return "You are already logged in.\n" +
                    $"To switch accounts enter 'logout {serverClient.CUser.ID}'";
            Console.ForegroundColor = ConsoleColor.Gray;
            return "<NoH>User: #/C.RL/##/C.NL/#Password: #/C.RL/#<<login>>";
        }
        public static string Logout(ServerClient serverClient)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" Client request to log out. ");

            if (serverClient.CUser == null) return "You are not logged in.\n" +
                    "To log in enter 'login'";
            Console.ForegroundColor = ConsoleColor.Gray;
            serverClient.CUser = new User(null, "");
            return "You have logged out.";
        }
    }
    public static class ClosedCommands
    {
        public static bool LoginAttempt(ServerClient serverClient, string[] details)
        {
            User user = UserFactory.GetUser(details[0]);
            if (user.ID == details[0])
                if (user.CheckPassword(details[1]))
                {
                    serverClient.CUser = user;
                    return true;
                }
            return false;
        }
    }
    public static class UserCommands
    {
        public static string directoryPath = null;
        public static string GetAllClients(ServerClient serverClient, string[] args = null)
        {
            if (!serverClient.CUser.AllowedAccess(0)) return "Access Level not high enough.";

            string clientList = null;
            int clientPos = 1;
            foreach (ServerClient client in Server.clients)
            {
                string clientID = "Not logged in";
                if (client.CUser != null) clientID = client.CUser.ID;
                clientList += $"\n\nClient machine name: {client.MachineName}\n" +
                    $"Client IP: {client.IP}\n" +
                    $"User ID: {clientID}\n" +
                    $"Connection order: {clientPos}\n" +
                    $"Handle: {client.CSocket.Handle}\n" +
                    $"Ttl: {client.CSocket.Ttl}\n" +
                    $"Data value: {client.CSocket.Available}\n" +
                    $"Protocol type: {client.CSocket.ProtocolType}";

                clientPos++;
            }
            return clientList;
        }
        public static string KickClient(ServerClient serverClient, string[] strPos)
        {
            if (!serverClient.CUser.AllowedAccess(0)) return "Access Level not high enough.";

            if(!Int32.TryParse(strPos[2], out int pos)) return $"Must input a value between 1 and {Server.clients.Count}";                       
            if (Server.clients[pos - 1].CUser.ID == serverClient.CUser.ID) return "You cannot remove this client, disconnect instead."; 
            if (pos > 0 && pos <= Server.clients.Count)
            {
                Server.clients[pos - 1].CSocket.Disconnect(true);
                Server.clients.RemoveAt(pos - 1);
                return "Client removed - NOTE: client positions will now have changed";
            }
            return "Client position not found.";            
        }        
        public static string RestartServer(ServerClient serverClient, string[] confirmed)
        {
            if (serverClient.CUser.AllowedAccess(0))
            {
               if(confirmed[2] == "force")
               {
                   //Build restart function, flush everything, loop round to start
                   
                   return "Server Restarted.";
               }
                return "Restart requires 'force', to confrim call.";
            }
            return "Access Level not high enough.";
        }
        public static string SeePersonalInfo(ServerClient serverClient, string[] args = null)
        {
                    return $"\n\nID: {(serverClient.CUser.ID ?? "Not logged in")}\n" +
                        $"\nConnection order: " +
                        $"{Server.clients.FirstOrDefault(client => client.CUser.ID == serverClient.CUser.ID).CUser.ID}\n" +
                        $"\nEnd point: {serverClient.CSocket.LocalEndPoint}\n" +
                        $"Handle: {serverClient.CSocket.Handle}\n" +
                        $"Ttl: {serverClient.CSocket.Ttl}\n" +
                        $"Data value: {serverClient.CSocket.Available}\n" +
                        $"Protocol type: {serverClient.CSocket.ProtocolType}";
        }
        public static string SendFileToServer(ServerClient serverClient, string[] fileData)
        {
            if (!serverClient.CUser.AllowedAccess(1)) return "Access Level not high enough.";

            return "Function to be added";
        }
        public static string RecieveFileFromServer(ServerClient serverClient, string[] fileData)
        {
            if (!serverClient.CUser.AllowedAccess(1)) return "Access Level not high enough.";

            return "Function to be added";
        }
        public static string SetServerDirPath(ServerClient serverClient, string[] fileData)
        {
            if (!serverClient.CUser.AllowedAccess(0)) return "Access Level not high enough.";

            if (fileData.Length < 2 || String.IsNullOrWhiteSpace(fileData[2]))
                return "HELP: set or change the server directory path with this command.\n" +
                    $"{((directoryPath == null) ? "No directory set, to send or recieve files a directory needs to be set." : $"Current directory: '{directoryPath}'")}";

            if (Directory.Exists(fileData[2]))
            {
                directoryPath = fileData[2];
                return $"Directory successfully changed to : {fileData[2]}";
            }
            else return "Invalid path, directory not found";

        }
    }
}

