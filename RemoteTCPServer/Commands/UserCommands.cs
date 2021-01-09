using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer.Commands
{

    public class UserCommands : IClientCommand
    {

        private static readonly List<Cmd> _commands = CommandFactory.GetUser();

        internal string directoryPath = null;

        public List<Cmd> Commands() => _commands;

        public string Call(ServerClient clientOwner, string data)
        {
            string[] subCommands = data.Split(' ');

            if(subCommands.Length < 2) return SyntaxError(subCommands);

            var function = _commands.Find(c => c.Key == subCommands[1]);
            if (function != null && function.Access >= clientOwner.CUser.GetAccess())
                return function.Execute(clientOwner, subCommands);

            return SyntaxError(subCommands);
        }
        public string GetHelp(ServerClient clientOwner)
        {
            return "Only Logged in clients can use these commands.\n" +
                "Some commands may not be visible, if you do not meet the access requirements.";
        }

        public string SyntaxError(string[] input)
        {
            return $"Command was not recogised: '{string.Join(' ', input)}' was not recognised as a listed command.";
        }

        internal string GetAllClients(ServerClient clientOwner)
        {
            if (!clientOwner.CUser.AllowedAccess(0)) return "Access Level not high enough.";

            string clientList = null;
            int clientPos = 1;
            foreach (ServerClient client in SP.Clients)
            {
                clientList += GetClientInfo(client);
                clientPos++;
            }
            return clientList;
        }
        internal string KickClient(ServerClient clientOwner, string[] strPos)
        {
            if (!clientOwner.CUser.AllowedAccess(0)) return "Access Level not high enough.";

            if (!Int32.TryParse(strPos[2], out int pos)) return $"Must input a value between 1 and {SP.Clients.Count}";
            if (SP.Clients[pos - 1].CUser.ID == clientOwner.CUser.ID) return "You cannot remove this client, disconnect instead.";
            if (pos > 0 && pos <= SP.Clients.Count)
            {
                SP.Clients[pos - 1].CSocket.Disconnect(true);
                SP.Clients.RemoveAt(pos - 1);
                return "Client removed - NOTE: client positions will now have changed";
            }
            return "Client position not found.";
        }
        internal string RestartServer(ServerClient clientOwner, string[] confirmed)
        {
            if (clientOwner.CUser.AllowedAccess(0))
            {
                if (confirmed[2] == "force")
                {
                    if (Server.Restart()) return "Server Restarted.";
                    else return "Error, server could not be restarted.";
                }
                return "Restart requires 'force', to confrim call.";
            }
            return "Access Level not high enough.";
        }
        internal string GetClientInfo(ServerClient client)
        {
            string id = "Not logged in";
            if (client.CUser != null) id = client.CUser.ID;

            return $"\n\nID: {id}\n" +
                $"IP address: {client.IP}\n" +
                $"Machine name: {client.MachineName}\n" +
                $"Connection order: {SP.Clients.IndexOf(client)}\n" +
                $"Handle: {client.CSocket.Handle}\n";
        }
        internal string FileSentToServer(string[] fileData)
        {
            string dirMsg = $"{((directoryPath == null) ? "No directory set, must be created by admin first" : "")}";
            string help = "HELP: Send a file of specified type to the server to be saved permanently." + dirMsg +
                $"{((dirMsg == null) ? "\n" : "")}  Syntax: '<userID> sendfile <full file path>'. The path must be a location on this machine.";

            if (fileData.Length < 4) return help;
            if (String.IsNullOrWhiteSpace(directoryPath)) return $"Error: {dirMsg}";

            string fileName = fileData[2];
            string fileContent = null;
            for (int i = 3; i < fileData.Length; i++) fileContent += fileData[i] + " ";

            byte[] fileBytes = Encoding.ASCII.GetBytes(fileContent);

            try
            {
                File.WriteAllBytes(directoryPath + fileName, fileBytes);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
                return "Could not save file to server";
            }
            return "File saved to server successfully";
        } 
        internal string FileByClientRequest(string[] fileData)
        {
            string dirMsg = $"{((directoryPath == null) ? "No directory set, must be created by admin first" : "")}";
            string help = "HELP: Recieve a file of specified type from the server to be saved permanently on this machine." + dirMsg +
                $"{((dirMsg == null) ? "\n" : "")}  Syntax: '<userID> sendfile <file name>'. File name only, '*.fileType' must be included.";

            if (fileData.Length < 4) return help;
            if (String.IsNullOrWhiteSpace(directoryPath)) return $"Error: {dirMsg}";
            if (!File.Exists(directoryPath + fileData[2])) return "Invalid file name, not found in server directory";
            if (File.ReadAllBytes(directoryPath + fileData[2]).Length > SP.MaxBufferSize) return "File too large to send.";

            if (!fileData[3].EndsWith('/')) fileData[3] += '/';
            return $"<<fileTransfer>> {fileData[3] + fileData[2]} {Encoding.ASCII.GetString(File.ReadAllBytes(directoryPath + fileData[2]))}";
        }
        internal string SetServerDirPath(string[] fileData)
        {
            string help = "HELP: set or change the server directory path with this command.\n" +
                    $"{((directoryPath == null) ? "No directory set, to send or recieve files a directory needs to be set." : $"Current directory: '{directoryPath}'")}";

            if (fileData.Length < 3) return help;
            if (String.IsNullOrWhiteSpace(fileData[2])) return help;
            if (Directory.Exists(fileData[2]))
            {

                directoryPath = fileData[2].Replace('\\', '/');
                if (directoryPath.Substring(directoryPath.Length - 1, 1) != "/") directoryPath += "/";
                return $"Directory successfully changed to : {fileData[2]}";
            }
            else return "Invalid path, directory not found";

        }
        internal string Messaging(ServerClient clientOwner, string[] fileData)
        {
            if (clientOwner.CUser.SecurityState.Messaging == 0) return $"Your messaging security state is set to '{clientOwner.CUser.SecurityState.GetState(0)}'\nIt must be changed before using this function.";

            string help = "Message other active clients\n" +
                "Clients must also be logged in to recieve messages and not have their security set to 'private'\n\n" +
                $"Syntax: {fileData[0]} message <ip>:<socketHandle> <your message>\n" +
                $"   e.g. {fileData[0]} message {clientOwner.IP}:{clientOwner.CSocket.Handle} Hello there!\n\n" +
                $"To list all contactable clients:\n" +
                $"Syntax: {fileData[0]} message list";

            (List<ServerClient>, string) openClients = OpenClientListRequest(clientOwner);

            if (fileData.Length == 3) if (fileData[2] == "list") return openClients.Item2;
            if (fileData.Length < 4) return help;
            if (!IPAddress.TryParse(fileData[2].Split(':')[0], out IPAddress targetIP)) return "Invalid syntax for IPv4 Address";

            ServerClient target = openClients.Item1.Find(
                c => c.CSocket.Handle.ToString() == fileData[2].Split(':')[1] &&
                c.IP.ToString() == targetIP.ToString());
            if (target == null) return $"Client not found.\n Connected clients (logged in):\n {openClients.Item2}";

            if (SameMachine(target, clientOwner)) return "You cannot message yourself.";

            string message = null;
            for (int i = 3; i < fileData.Length; i++) message += $"{fileData[i]} ";

            string allData = $"<<client[{clientOwner.IP}:{clientOwner.CSocket.Handle} ({clientOwner.CUser.ID})].message{MessageClient(target.CUser.SecurityState.Messaging)}>> " +
                $"{message.Trim()}";

            Server.PrepareSend(SP.Clients.IndexOf(clientOwner), allData, target.CSocket);
            return "Message sent";
        }
        private (List<ServerClient>, string) OpenClientListRequest(ServerClient currentClient, bool messaging = true)
        {
            string ipList = "list:\n";
            List<ServerClient> list = new();
            foreach (ServerClient client in SP.Clients)
                if (client.CUser != null)
                {
                    if ((messaging && client.CUser.SecurityState.Messaging != 0) || (!messaging && client.CUser.SecurityState.FileTransfer != 0))
                    {
                        list.Add(client);
                        ipList += $"{client.IP}:{client.CSocket.Handle} {(SameMachine(client, currentClient) ? "<- your machine" : "")}\n";
                    }
                }
            return (list, ipList);
        }
        private bool SameMachine(ServerClient one, ServerClient two)
        {
            if ($"{one.IP}:{one.CSocket.Handle}" == $"{two.IP}:{two.CSocket.Handle}")
                return true;
            return false;
        }
        private string MessageClient(int promptRequired)
        {
            if (promptRequired == 1) return "|request";
            else if (promptRequired != 2) throw new ArgumentOutOfRangeException("client security settings out of range");
            return "";
        }
        internal string AddNewUser(string[] fileData)
        {
            string help = "Add a new temporary user to the server, this user will not be stored once the server closes.\n" +
               "Only the admin can create a new user\n\n" +
               $"Syntax: admin addnewuser <newuserID> <newuserPassword>\n" +
               $"   e.g. admin addnewuser owen steele\n\n" +
               $"This creates a new user, to login to this user as a client:\n" +
               $"& login\n" +
               $"user: owen\n" +
               $"password: steele\n\n" +
               $"A new user has by default basic-elevated privileges.";

            if (fileData.Length < 4) return help;
            string[] details = { fileData[2], fileData[3] };

            if (UserFactory.AddUser(details)) return "New user added successfully.";
            else return "Error new user could not be added.";
        }
        internal string UserSecurity(ServerClient clientOwner, string[] fileData)
        {
            if (clientOwner.CUser == null) return "Access Level not high enough.";

            string help = "\n---Current Security settings---\n" +
               $"Messaging: {clientOwner.CUser.SecurityState.GetState(0)}\n" +
               $"File Transfer: {clientOwner.CUser.SecurityState.GetState(1)}\n\n" +
               $"To change your security setting enter:\n" +
               $"   Syntax: {clientOwner.CUser.ID} security messaging [n]\n" +
               $"   Syntax: {clientOwner.CUser.ID} security filetransfer [n]\n" +
               $"      where [n] = 0, 1 or 2\n" +
               $"      0 = private (cannot be contacted), 1 = prompt me on request, 2 = auto accept any requests.\n\n" +
               $"   e.g. {clientOwner.CUser.ID} security messaging 1\n" +
               $"   sets messaging to 'on prompt'.\n\n";

            if (fileData.Length < 4) return help;

            int securityState;

            if (fileData[2] == "messaging")
            {
                if (!Int32.TryParse(fileData[3], out securityState)) return "Invalid entry for security state, must be 0, 1, 2, only.";
                if (securityState >= 0 && securityState <= 2)
                {
                    clientOwner.CUser.SecurityState.Messaging = securityState;
                    return $"Messaging security changed to: {clientOwner.CUser.SecurityState.GetState(0)}";
                }
            }
            else if (fileData[2] == "messaging")
            {
                if (!Int32.TryParse(fileData[3], out securityState)) return "Invalid entry for security state, must be 0, 1, 2, only.";
                if (securityState >= 0 && securityState <= 2)
                {
                    clientOwner.CUser.SecurityState.FileTransfer = securityState;
                    return $"Messaging security changed to: {clientOwner.CUser.SecurityState.GetState(1)}";
                }
            }
            else return "Invalid security call\n\n" + help;
            return "Error security could not be changed.";
        }

        internal string GetSqlCommands(ServerClient clientOwner, string[] input)
        {
            return clientOwner.SqlCommands.Call(clientOwner, string.Join(' ',input));
        }
    }
}
