using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer.Commands
{
    public class ClosedCommands : ICommand
    {
        private static readonly List<Cmd> _commands = CommandFactory.GetClosed();

        internal string LoginAttempt(ServerClient clientOwner, string[] details)
        {
            User user = UserFactory.GetUser(details[0]);

            if (user.ID != details[0])  return "Error, could not log in";

            if (user.CheckPassword(details[1]))
            {
                clientOwner.CUser = user;
            }
            return "Success, you are now logged in ";
        }

        public string Call(ServerClient clientOwner, string data)
        {
            if (data == null) return null;

            if (!(data.Contains("<<") && data.Contains(">>"))) return null;

            string functionTag = data.Substring(data.IndexOf("<<"), (data.IndexOf(">>") - data.IndexOf("<<")) + 2);
            int endCloseTag = data.IndexOf(">>") + 2;
            string request = data[endCloseTag..];

            string[] subcommands = request.Split("|");

            var function = _commands.Find(c => c.Key == functionTag);

            if (function == null) return "ERROR"; 


                
            return function.Execute(clientOwner, subcommands);

            
        }

        public List<Cmd> Commands() => _commands;
    }
}
