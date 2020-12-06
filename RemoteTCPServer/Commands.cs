using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public static class OpenCommands
    {
        public static string currentTag;
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
        public static string GetAllClients(ServerClient serverClient)
        {

            if (serverClient.CUser.AllowedAccess(0))
            {
                string clientList = null;
                foreach(ServerClient client in Server.clients)
                {
                    clientList += $"\nEnd point: {client.CSocket.LocalEndPoint}\n" +
                        $"Handle: {client.CSocket.Handle}\n" +
                        $"Ttl: {client.CSocket.Ttl}\n" +
                        $"Data value: {client.CSocket.Available}\n" +
                        $"Protocol type: {client.CSocket.ProtocolType}";
                }
                return clientList;
            }
            
            return "Access Level not high enough.";
        }
    }
}

