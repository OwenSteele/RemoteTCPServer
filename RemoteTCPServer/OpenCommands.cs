using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public static class OpenCommands
    {
        public static string currentTag;
        public static string GetTime() => DateTime.Now.ToLongTimeString();
        public static string Login()
        {
            Console.WriteLine("Client request to log on.");
            if (currentTag == "<<Login1>>") return "<NoH>Password: <<login2>>";
            if (currentTag == "<<Login2>>") return "<NoH>Logged in!";
            return "<NoH>User: <<login1>>";
        }
    }
}
