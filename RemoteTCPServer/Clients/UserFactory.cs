using System.Collections.Generic;

namespace RemoteTCPServer
{
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
        internal static User GetUser(string id) => _users.Find(u => u.ID == id) ?? _users[0];

        internal static bool AddUser(string[] details) 
        {
            ///[0]=ID, [1]=pwd
            if (_users.Find(u => u.ID == details[0]) == null)
            {
                _users.Add(new User(details[0], details[1], 1));
                return true;
            }                
            return false;
        }
    }
}
