namespace RemoteTCPServer.Commands
{
    public interface IClientCommand :ICommand
    {
        public string GetHelp(string[] args);
        public string SyntaxError(string[] input);

    }
}
