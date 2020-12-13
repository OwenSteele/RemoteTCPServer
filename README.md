# <ins>Remote TCP Server</ins>
# See also *ReadMe_RemoteTCPServer.ipynb*

##### By Owen Steele 2020

*This repo works alongside the other repo ./RemoteTCPClient.git*

#### <ins>Functionality</ins>
This Server runs on local networks only, with multiple clients connected at once.

Can select the Port to listen and accept client through.

Runs solely as an output and logging application, a client with admin priviliges is required to alter any properties or parameters.

SSL encryption enabled, this requires a valid certificate for the server.
SSL is optional, can run without if a certifate is not available or obtainable.

### Setup and Running
##### .NET 5.0 runtime is required to execute.
Can be downloaded from MDN here: https://dotnet.microsoft.com/download
Select .NET [core/framework] runtime option and install

<ins>Simple setup through GitHub</ins>
```
1. Download this repo as a '.zip' file, and extract once downloaded.
2. Navigate to /RemoteTCPServer/bin/Debug/net5.
3. Run RemoteTCPServer.exe
```

**Can also be built and run with Visual Studio**

Ensure that your machine has the .NET 5.0 SDK installed

<ins>Simple setup through terminal (windows)</ins>
```
1. cd [your chosen dir]
2. mkdir RemoteTCPServer
3. cd RemoteTCPServer 
4. git init
5. git clone https://github.com/OwenSteele/RemoteTCPServer.git
6. cd /RemoteTCPServer/bin/Debug/net5.0/
7. ./RemoteTCPServer.exe
```
*NOTE: step 5 requires the necessary access privileges*

### Creating the server
The server will ask you for initial input:

* Enter the port number: (0 to 65535)
* Choose to enable SSL encryption:
    If you enable SSL, you will be prompted to provide the path and file name of the correct certificate.
    You will then be promted to set the 'key' or server name:
    
The Server will then obtain it's external IP and print its metadata.
**Clients require the localIP and the port number to connect.**
**Clients are not required to enable SSL to connect - however if enabled, clients also require the server name (key) set.**

### Stucture
```
private static void CallBack(IAsyncResult ar);
```
C# Asyncronous CallBack method are used to handle multiple clients

### Clients
**Connect:** On connectiong a client's metadata is stored as it's key and stored in the connected clients list.

**Commands:** Client have a range of functions that can be called through requests, the server stores all of the commands.

**Logging:** Client requests to the server are logged in Yellow.
             Server login outputs are logged in Cyan.
             Server replies to the client are logged in DarkCyan.

**Users:** Client can log in to users, this data is solely held by the server.
           When a client calls a user only command, the server will process whether the client hold the necessary privileges.
    
**Disconnect:** If a client disconnects, this is logged in white, the client is removed from the list of connected clients.

<ins> **User commands**</ins>

*after a successful login attempt, clients have a speficied user bound to them by the server*

**File Transfer:** Users can send and retrieve files from the server (requires admin dir set first).

**Messaging:** Users can directly message one another, with the server acting as an intermediary.

**Security:** Users can set their visiblity and security to three states, 'private', 'prompt on request' and auto accept'.
              Private users cannot recieve or send anything to other clients, and cannot be seen by other clients.
              Prompted users, must accept a request from another client before recieving data, does not apply to sending data.
              Auto accepting users, automatically accept any data sent to them.


