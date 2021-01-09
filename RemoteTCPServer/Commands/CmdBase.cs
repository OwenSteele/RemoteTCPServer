namespace RemoteTCPServer.Commands
{
    public class CmdBase
    {
        public string Key { get; set; }
        public string Info { get; set; }
        public int Access { get; set; }

        public CmdBase(string key, string commandInfo, int? accessLevelReq = null)
        {
            Key = key;
            Info = commandInfo;
            Access = accessLevelReq ?? 255;

        }
    }
}