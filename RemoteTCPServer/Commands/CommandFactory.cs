using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer.Commands
{
    public static class CommandFactory
    {
        private static List<Cmd> _openCommands;
        private static List<Cmd> _closedCommands;
        private static List<Cmd> _userCommands;
        private static List<Cmd> _sqlCommands;

        public static void CreateCommands()
        {
            _openCommands = CreateOpenCommands();
            _closedCommands = CreateClosedCommands();
            _userCommands = CreateUserCommands();
            _sqlCommands = CreateSqlCommands();
        }
        private static List<Cmd> CreateOpenCommands()
        {
            List<Cmd> commands = new();
            OpenCommands openCommands = new();

            commands.Add(new Cmd("help",       openCommands.GetHelp,       "lists all of the available commands, and syntaxing"));
            commands.Add(new Cmd("serverinfo", openCommands.ServerDetails, "returns the information about the host"));
            commands.Add(new Cmd("gettime",    openCommands.GetTime,       "returns the current time and date, local to host"));
            commands.Add(new Cmd("login",      openCommands.Login,         "Enables a client to login to a user"));
            commands.Add(new Cmd("logout",     openCommands.Logout,        "If a client is logged in, executes logout.")); 

            return commands;
        }
        private static List<Cmd> CreateClosedCommands()
        {
            List<Cmd> commands = new();
            ClosedCommands closedCommands = new();

            commands.Add(new Cmd("<<login>>", closedCommands.LoginAttempt, "SERVER SIDE ONLY - accessed internally when a client attempts to login"));

            return commands;
        }
        private static List<Cmd> CreateUserCommands()
        {
            List<Cmd> commands = new();
            UserCommands userCommands = new();

            commands.Add(new Cmd("listclients",   userCommands.GetAllClients,       "Returns connection info for every client", 0));
            commands.Add(new Cmd("clientinfo",    userCommands.GetClientInfo,       "Returns connection info about this client", 1));
            commands.Add(new Cmd("kickclient",    userCommands.KickClient,          "TBI - Terminates a clients connection to the host", 0));
            commands.Add(new Cmd("serverrestart", userCommands.RestartServer,       "IIP - Restarts the server entity, currently cannot be done remotely", 0));
            commands.Add(new Cmd("sendfile",      userCommands.FileSentToServer,    "Transfers a file from the client to the host (req: dir to be set)", 1));
            commands.Add(new Cmd("getfile",       userCommands.FileByClientRequest, "Transfers a file from the host to the client (req: dir to be set)", 1));
            commands.Add(new Cmd("setdir",        userCommands.SetServerDirPath,    "Sets the path for file transfer on the host machine, required for host-client file transfer", 0));
            commands.Add(new Cmd("message",       userCommands.Messaging,           "Sends a message to another connected client, only non-private clients can be messaged", 2));
            commands.Add(new Cmd("addnewuser",    userCommands.AddNewUser,          "Adds a server-entity-lifetime user.", 0));
            commands.Add(new Cmd("security",      userCommands.UserSecurity,        "Alters the security setting for this client (messaging and file transfer currently supported)", 1));
            commands.Add(new Cmd("sql",           userCommands.GetSqlCommands,      "Interact with the attached SQL API to issue SQL commands to a database (req: 'sql init' command)", 1));

            return commands;
        }
        private static List<Cmd> CreateSqlCommands()
        {
            List<Cmd> commands = new(); 
            SqlCommands sqlCommands = new();

            commands.Add(new Cmd("command", sqlCommands.NonQuery,   "Send a non-query based command to the connected SQL database - Does not listen for a reply from the database",1));
            commands.Add(new Cmd("query",   sqlCommands.Query,      "Send a query to the connected SQL database - Returns the response from the database", 1));
            commands.Add(new Cmd("init",    sqlCommands.Initialize, "Initializes and connects to a SQL database entity, see command help. Required to send [non-]queries",0));

            return commands;
        }

        public static List<Cmd> GetOpen() => _openCommands;
        public static List<Cmd> GetClosed() => _closedCommands;
        public static List<Cmd> GetUser() => _userCommands;
        public static List<Cmd> GetSql() => _sqlCommands;
        public static List<(Cmd,string)> GetAll(bool includeClosed = false)
        {
            List<(Cmd, string)> allCommands = new();

            foreach (var command in _openCommands) allCommands.Add(new(command, "open"));
            foreach (var command in _userCommands) allCommands.Add(new(command, "user"));

            if(includeClosed) 
                foreach (var command in _closedCommands) 
                    allCommands.Add(new(command, "open"));

            return allCommands;

        }
    }
}
