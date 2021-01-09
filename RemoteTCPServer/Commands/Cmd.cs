using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer.Commands
{
    public sealed class Cmd : CmdBase
    {     
        //public Func<ServerClient, string[], string> Execute { get; set; }
        private Func<ServerClient, string[], string> _functionSCD { get; set; }
        private Func<ServerClient, string> _functionSC { get; set; }
        private Func<string[], string> _functionD { get; set; }
        private Func<string> _function { get; set; }

        public Cmd(string key, Func<ServerClient, string[], string> functionPair,
            string commandInfo, int? accessLevelReq = null) : base (key,commandInfo,accessLevelReq)
        {
            _functionSCD = functionPair;
        }
        public Cmd(string key, Func<ServerClient, string> functionPair,
           string commandInfo, int? accessLevelReq = null) : base(key, commandInfo, accessLevelReq)
        {
            _functionSC = functionPair;
        }
        public Cmd(string key, Func<string[], string> functionPair,
           string commandInfo, int? accessLevelReq = null) : base(key, commandInfo, accessLevelReq)
        {
            _functionD = functionPair;
        }
        public Cmd(string key, Func<string> functionPair,
           string commandInfo, int? accessLevelReq = null): base (key,commandInfo,accessLevelReq)
        {
            _function = functionPair;
        }

        public string Execute(ServerClient serverClient, string[] data)
        {
            if (_functionSCD != null) return _functionSCD(serverClient,data);
            if (_functionSC != null) return _functionSC(serverClient);
            if (_functionD != null) return _functionD(data);
            return _function();
        }
        public string Execute(ServerClient serverClient, string data) => 
            Execute(serverClient, data.Split(' '));
    }
}
