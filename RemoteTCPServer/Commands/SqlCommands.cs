using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer.Commands
{
    public class SqlCommands : IClientCommand
    {
        private List<Cmd> _commands = CommandFactory.GetSql();

        public static ServerClient ClientOwner { get; internal set; }

        public int MinCommands { get; } = 3;

        public string Call(string request)
        {
            string[] input = request.Split(' ');
            
            if (String.IsNullOrWhiteSpace(input[2])) return GetHelp();

            var function = _commands.Find(c => c.Key == input[2]);
            
            string[] data = new string[input.Length - 3];
            for (int i = 3; i < input.Length; i++) data[i-3] = input[i];

            if (!data.Last().EndsWith(';')) return SyntaxError(new string[] { "SQL syntax error: must end in with semicolon ';'" });

            if (function != null && ClientOwner.CUser.GetAccess() <= function.Access) 
                return function.Execute(input);

            return SyntaxError(request.Split(' '));                 
        }

        internal string Initialize(string[] data)
        {
            if (SP.SqlInfo[0].Length == 0 || data == null) return "SQL server details must be setup first. Use command:\n" +
                             $"{ClientOwner.CUser.ID} 'sql init <databaseName> <user id> <user password> <optional:hostname>'\n" +
                             $"e.g. {ClientOwner.CUser.ID} 'sql init database1 root password' - optionals are set to their default values.";

            if (ClientOwner.SqlClient.SQLCommand(SP.SqlInfo, string.Join(' ', data))) return "SQL command successful";
            return "Could not complete SQL command";
        }

        internal string Query(string[] data)
        {

            if (SP.SqlInfo[0].Length == 0) return "SQL server details must be setup first. Use command:\n" +
                            $"{ClientOwner.CUser.ID} 'sql init <databaseName> <user id> <user password> <optional:hostname>'\n" +
                            $"e.g. {ClientOwner.CUser.ID} 'sql init database1 root password' - optionals are set to their default values.";

            if (data == null) return "Returns data related to query from current SQL server\n" +
                 "1. table name";

            string result = ClientOwner.SqlClient.Query(SP.SqlInfo, string.Join(' ',data));
            if (result == null) return $"Could not complete SQL query, check '{data}' contains a valid table name";
            return result;
        }

        internal string NonQuery(string[] input)
        {
            List<string> optionals = new();
            if (input.Length < 5) return "To initialize a MySQL database server connection, you must include the \n" +
                    "1.userid\n" +
                    "2.password\n" +
                    "3.hostname [optional if port is 3006 too, default = 'localhost']\n" +
                    "4.port [optional, default = '3306']\n" +
                    "    Syntax: '{serverClient.CUser.ID} sql init userID password'\n" +
                    "            '{serverClient.CUser.ID} sql init userID password hostName'\n" +
                    "            '{serverClient.CUser.ID} sql init userID password hostName portNumber'\n";

            else for (int e = 6; e < input.Length; e++) optionals.Add(input[e]);

            SP.SqlInfo[0] = (optionals.Count == 0) ? "localhost" : optionals[0];
            SP.SqlInfo[1] = (optionals.Count == 1) ? "3306" : optionals[1];
            SP.SqlInfo[2] = input[3];
            SP.SqlInfo[3] = input[4];

            return "Server data stored - 'WARNING: this has not been checked, errors will only become apparent when trying to contact the server'\n\n" +
                "You must switch a to database upon connection!\n" +
                $"   '{ClientOwner.CUser.ID} sql command USE <databasename>'";
        }

        public string GetHelp(string[] args = null)
        {
            string availableCommands = null;
            foreach (var command in _commands)
            {
                if(ClientOwner.CUser.GetAccess() <= command.Access) availableCommands += $"    {command.Key} - {command.Info}\n";
            }

            return "\nHELP: sql commands\n" +
                "LIST (commands above your access level are not shown):\n" +
                $"{availableCommands}\n" +
                $"Syntax: {ClientOwner.CUser.ID} sql <command>\n" +
                $"To see the 'help' for each of these commands type only the above,\n" +
                $"        e.g. '{ClientOwner.CUser.ID} sql query\n\n" +
                $"All sql queries must use sql syntax and end with a ';'\n" +
                $"        e.g. '{ClientOwner.CUser.ID} sql query DESCRIBE exampletable;'";
        }

        public string SyntaxError(string[] input)
        {
            return $"Command was not recogised: '{string.Join(' ', input)}' was not recognised as a listed command.";
        }

        public List<Cmd> Commands()
        {
            throw new NotImplementedException();
        }
    }
}
