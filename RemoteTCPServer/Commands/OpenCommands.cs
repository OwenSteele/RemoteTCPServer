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
        private List<Cmd> _commands = CommandFactory.GetOpen();

        public static ServerClient ClientOwner { get; internal set; }

        public string CurrentTag { get; set; } = null;

        internal string ServerDetails(string[] args = null)
        {
            return "---Server details---" +
                   $"\n     External IP: {SP.ExternalIP}" +
                   $"\n     Local IP: {SP.GetLocalIPAddress()}" +
                   $"\n     Port number: {SP.ServerPort}" +
                   $"\n{(SP.SslEnabled ? $"     SSL server name : '{SP.ServerName}'" : "")}";
        }
        internal string GetTime(string[] args = null)
        {
            return DateTime.Now.ToLongTimeString();
        }
        internal string Login(string[] args = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Client request to log on. ");

            if (ClientOwner.CUser != null) return "You are already logged in.\n" +
                    $"To switch accounts enter 'logout {ClientOwner.CUser.ID}'";

            Console.ForegroundColor = ConsoleColor.Gray;
            return "<NoH>User: #/C.RL/#Password: #/C.RL/#<<login.MR>>";
        }
        internal string Logout(string[] args = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Client request to log out. ");

            if (ClientOwner.CUser == null) return "You are not logged in.\n" +
                    "To log in enter 'login'";

            Console.ForegroundColor = ConsoleColor.Gray;
            ClientOwner.CUser = new User(null, "");
            return "You have logged out.";
        }

        public string GetHelp(string[] args = null)
        {
            string help = "\n-----All available commands-----\n";

            foreach(var command in _commands)
            {
                help += $"{command.Key} - '{command.Info}'\n";
            }

            if (ClientOwner.CUser == null) return help;

            help += $"\n\n--USER ONLY COMMANDS--\n" +
                $"  Must be preceded by the user ID\n" +
                $"e.g. '{ClientOwner.CUser.ID} <command>'\n";

            var userCommands = CommandFactory.GetUser();

            foreach (var userCommand in userCommands)
            {
                if (ClientOwner.CUser.AllowedAccess(userCommand.Access))
                {
                    help += $"{userCommand.Key} - '{userCommand.Info}'\n";
                }
            }

            return help;
        }
        public string SyntaxError(string[] input)
        {
            return $"Command was not recogised: '{string.Join(' ',input)}' was not recognised as a listed command.";
        }

        public List<Cmd> Commands() => _commands;

        public string Call(string data)
        {
            var function = _commands.Find(c => c.Key == data);
            if (function != null) return function.Execute(null);

            return SyntaxError(data.Split(' '));
        }
    }
    }

