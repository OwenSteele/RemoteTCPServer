namespace RemoteTCPServer.Commands
{
    public interface IClientCommand :ICommand
    {
        public string GetHelp(ServerClient clientOwner = null);
        public string SyntaxError(string[] input = null);

    }
}
