using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace RemoteTCPServer.Commands
{
    public class OpenCommands : IClientCommand
    {
        private static readonly List<Cmd> _commands = CommandFactory.GetOpen();

        public string CurrentTag { get; set; } = null;

        internal string ServerDetails()
        {
            return "---Server details---" +
                   $"\n     External IP: {SP.ExternalIP}" +
                   $"\n     Local IP: {SP.GetLocalIPAddress()}" +
                   $"\n     Port number: {SP.ServerPort}" +
                   $"\n{(SP.SslEnabled ? $"     SSL server name : '{SP.ServerName}'" : "")}";
        }
        internal string GetTime()
        {
            return DateTime.Now.ToLongTimeString();
        }
        internal string Login(ServerClient clientOwner)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Client request to log on. ");
             
            if (clientOwner.CUser != null) //Double check required as CUser cannot be set to null on logout, must check ID
                if (clientOwner.CUser.ID != null) return "You are already logged in.\n" +
                    $"To switch accounts enter 'logout {clientOwner.CUser.ID}'";

            Console.ForegroundColor = ConsoleColor.Gray;
            return "<NoH>User: #/C.RL/#Password: #/C.RL/#<<login.MR>>";
        }
        internal string Logout(ServerClient clientOwner)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Client request to log out. ");

            if (clientOwner.CUser == null) return "You are not logged in.\n" +
                    "To log in enter 'login'";

            Console.ForegroundColor = ConsoleColor.Gray;
            clientOwner.CUser = new User(null, "");
            return "You have logged out.";
        }

        public string GetHelp(ServerClient clientOwner)
        {
            string help = "\n-----All available commands-----\n";

            foreach(var command in _commands)
            {
                help += $"{command.Key} - '{command.Info}'\n";
            }

            if (clientOwner.CUser == null) return help;

            help += $"\n\n--USER ONLY COMMANDS--\n" +
                $"  Must be preceded by the user ID\n" +
                $"e.g. '{clientOwner.CUser.ID} <command>'\n";

            var userCommands = CommandFactory.GetUser();

            foreach (var userCommand in userCommands)
            {
                if (clientOwner.CUser.AllowedAccess(userCommand.Access))
                {
                    help += $"{userCommand.Key} - '{userCommand.Info}'\n";
                }
            }

            return help;
        }
        public string SyntaxError(string[] input = null)
        {
            return $"Command was not recogised: '{string.Join(' ',input)}' was not recognised as a listed command.";
        }

        public List<Cmd> Commands() => _commands;

        public string Call(ServerClient clientOwner, string data)
        {
            var function = _commands.Find(c => c.Key == data);

            if (function == null) return SyntaxError(data.Split(' '));

            return function.Execute(clientOwner, data);
        }
    }
    }

