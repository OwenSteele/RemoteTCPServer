using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public sealed class User
    {
        public string ID { get; init; }
        public Security SecurityState { get; set; } = new();
        private byte[] _password;
        private int? _securityLevel;

        public User(string id, string fullPath, int? level = null)
        {
            ID = id;
            _password = Encoding.ASCII.GetBytes(fullPath); //needs to be changed to FileI(O)
            _securityLevel = level;
        }

        public bool AllowedAccess(int accessRequired) => (_securityLevel ?? 10000) <= accessRequired;
        public bool CheckPassword(string attempt) => _password.SequenceEqual(Encoding.ASCII.GetBytes(attempt));
        public int? GetAccess() => _securityLevel;
    }
}
