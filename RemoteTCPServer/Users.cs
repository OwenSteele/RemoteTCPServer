using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public sealed class User
    {
        public string ID { get; init;  }
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
    }
    public static class UserFactory
    {
        private static List<User> _users = new();
        public static void Create()
        {
            _users.Add(new User("guest", ""));
            _users.Add(new User("admin", "12345678", 0));
            _users.Add(new User("owen", "steele", 1));
        }
        internal static User GetUser(string id) => _users.FirstOrDefault(u => u.ID == id) ?? _users[0];
    }
}
