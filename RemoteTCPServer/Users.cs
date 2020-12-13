using System;
using System.Collections.Generic;
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
    public class Security
    {
        /// <summary>
        /// Client request policy: 0 = Private, 1 = Prompt, 2 = auto accept
        /// </summary>
        private int _messaging = 2;
        private int _fileTransfer = 2;
        public int Messaging
        {
            get { return _messaging; }
            set { _messaging = Range(value); }
        }
        public int FileTransfer
        {
            get { return _fileTransfer; }
            set { _fileTransfer = Range(value); }
        }
        public string GetState(int property)
        {
            int secValue = -1;
            if (property == 0) secValue = _messaging;
            else if (property == 1) secValue = _fileTransfer;
            else return "Invalid request.";

            Dictionary<int, string> states = new()
            {
                { 0, "Private" },
                { 1, "Prompt" },
                { 2, "Auto Accept" }
            };
            return states.GetValueOrDefault(Range(secValue));
        }

        private int Range(int x, int upper = 2)
        {
            if (x < 0 || x > upper) throw new ArgumentOutOfRangeException(
                message: (upper == 2) ? "Client request policy: 0 = Private, 1 = Prompt, 2 = auto accept" : "No property", null);
            return x;
        }
    }
    public static class UserFactory
    {
        private static List<User> _users = new();
        public static void Create()
        {
            _users.Add(new User("guest", ""));
            _users.Add(new User("admin", "12345678", 0));
            _users.Add(new User("owen", "steele", 1));
            _users.Add(new User("jack", "warren", 1));
            _users.Add(new User("user", "password", 1));
        }
        internal static User GetUser(string id) => _users.FirstOrDefault(u => u.ID == id) ?? _users[0];

        internal static bool AddUser(string[] details) 
        {
            ///[0]=ID, [1]=pwd
            if (_users.FirstOrDefault(u => u.ID == details[0]) == null)
            {
                _users.Add(new User(details[0], details[1], 1));
                return true;
            }                
            return false;
        }
    }
}
