using System;
using System.Linq;
using System.IO;
using System.Text;

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
            return "<NoH>User: #/C.RL/#Password: #/C.RL/#<<login.MR>>";
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
                clientList += GetClientInfo(client);
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
                    if(Server.Restart()) return "Server Restarted.";
                    else return "Error, server could not be restarted.";
                }
                return "Restart requires 'force', to confrim call.";
            }
            return "Access Level not high enough.";
        }
        public static string GetClientInfo(ServerClient serverClient, string[] args = null)
        {
                    return $"\n\nID: {(serverClient.CUser == null ? "Not logged in" : serverClient.CUser.ID)}\n" +
                        $"\nConnection order: " +
                        $"{Server.clients.FirstOrDefault(client => client.CSocket == serverClient.CSocket).CUser.ID}\n" +
                        $"\nEnd point: {serverClient.CSocket.LocalEndPoint}\n" +
                        $"Handle: {serverClient.CSocket.Handle}\n" +
                        $"Ttl: {serverClient.CSocket.Ttl}\n" +
                        $"Data value: {serverClient.CSocket.Available}\n" +
                        $"Protocol type: {serverClient.CSocket.ProtocolType}";
        }
        public static string FileSentToServer(ServerClient serverClient, string[] fileData)
        {
            string dirMsg = $"{((directoryPath == null) ? "No directory set, must be created by admin first" : "")}";
            string help = "HELP: Send a file of specified type to the server to be saved permanently." + dirMsg +
                $"{((dirMsg ==null) ? "\n" : "")}  Syntax: '<userID> sendfile <full file path>'. The path must be a location on this machine.";

            if (!serverClient.CUser.AllowedAccess(1)) return "Access Level not high enough.";

            if (fileData.Length < 4) return help;

            if (String.IsNullOrWhiteSpace(directoryPath)) return $"Error: {dirMsg}";

            //
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
            if (!serverClient.CUser.AllowedAccess(1)) return "Access Level not high enough.";

            if (!File.Exists("")) return "Invalid path, directory not found";

            byte[] fileBytes = File.ReadAllBytes(fileData[2]);
            if (fileBytes.Length > Server.maxBufferSize) return "File too large to send.";
            return "Function to be added";
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
                
                directoryPath = fileData[2].Replace('\\','/');
                if (directoryPath.Substring(directoryPath.Length - 1, 1) != "/") directoryPath += "/";
                return $"Directory successfully changed to : {fileData[2]}";
            }
            else return "Invalid path, directory not found";

        }
    }
}

