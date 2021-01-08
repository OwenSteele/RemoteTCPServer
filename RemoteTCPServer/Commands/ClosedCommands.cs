using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer.Commands
{
    public class ClosedCommands : ICommand
    {
        private List<Cmd> _commands = CommandFactory.GetClosed();

        public static ServerClient ClientOwner { get; internal set; }

        internal string LoginAttempt(string[] details)
        {
            User user = UserFactory.GetUser(details[0]);

            if (user.ID != details[0])  return "Error, could not log in";

            if (user.CheckPassword(details[1]))
            {
                ClientOwner.CUser = user;
            }
            return "Success, you are now logged in ";
        }

        public string Call(string data)
        {
            if (data == null) return null;

            if (!(data.Contains("<<") && data.Contains(">>"))) return null;

            string functionTag = data.Substring(data.IndexOf("<<"), (data.IndexOf(">>") - data.IndexOf("<<")) + 2);
            string request = data.Substring(data.IndexOf(">>") + 2, data.Length - (data.IndexOf(">>") + 2));

            string[] subcommands = request.Split("|");

            var function = _commands.Find(c => c.Key == functionTag);

            if (function != null) return function.Execute(subcommands);

            return "ERROR";
        }

        public List<Cmd> Commands() => _commands;
    }
}
