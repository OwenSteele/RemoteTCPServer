using System.Collections.Generic;
namespace RemoteTCPServer.Commands
{
    public interface ICommand
    {
        public List<Cmd> Commands();
        public string Call(ServerClient clientOwner, string data);
    }
}
