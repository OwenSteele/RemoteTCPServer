using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer.Commands
{
    public sealed class Cmd
    {
        public string Key { get; set; }        
        public Func<string[], string> Execute { get; set; }
        public string Info { get; set; }
        public int Access { get; set; }

        public Cmd(string key, Func<string[], string> functionPair,
            string commandInfo, int? accessLevelReq = null)
        {
            Key = key;
            Execute = functionPair;
            Info = commandInfo;
            Access = accessLevelReq ?? 255;
        }

    }
}
