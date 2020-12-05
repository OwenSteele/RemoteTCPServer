using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;

namespace RemoteTCPServer
{
    public static class SSLCertification
    {        
        public static void CertifyClient(Socket socket, X509Certificate serverCertificate)
        {
            Console.WriteLine("Starting SSL client certification.");
            TcpClient client = new();
            client.Client = socket;
            SslStream sslStream = new SslStream(client.GetStream(), false);

            try
            {
                sslStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);

                DisplaySecurityLevel(sslStream);
                DisplaySecurityServices(sslStream);
                DisplayCertificateInformation(sslStream);
                DisplayStreamProperties(sslStream);

                sslStream.WriteTimeout = 10000; //high value
                sslStream.ReadTimeout = 10000;

                Console.WriteLine("Waiting for client SSL messsage.");
                Console.WriteLine($"Messaged received: {ReadMessage(sslStream)}");
                byte[] messageToClient = Encoding.ASCII.GetBytes("The server is authenticating your connection.");
                Console.WriteLine("Sending message to Client");
                sslStream.Write(messageToClient);
            }
            catch (AuthenticationException aEx)
            {
                Console.WriteLine($"Exception: {aEx.Message}");
                if (aEx.InnerException != null) Console.WriteLine($"Inner exception: {aEx.InnerException.Message}");
                Console.WriteLine("Authentication failed - closing the connection.");
                sslStream.Close();
                client.Close();
                return;
            }
            finally
            {
                sslStream.Close();
                client.Close();
            }
        }
        private static string ReadMessage(SslStream sslStream)
        {
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new ();
            int bytes = -1;
            do
            {
                bytes = sslStream.Read(buffer, 0, buffer.Length);
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];

                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                if (messageData.ToString().IndexOf("<EOF>") != -1) break;
            } while (bytes != 0);

            return messageData.ToString();
        }
        private static void DisplaySecurityLevel(SslStream stream)
        {
            Console.WriteLine($"Cipher: {stream.CipherAlgorithm} strength { stream.CipherStrength}");
            Console.WriteLine($"Hash: {stream.HashAlgorithm} strength { stream.HashStrength} ");
            Console.WriteLine($"Key exchange: {stream.KeyExchangeAlgorithm} strength { stream.KeyExchangeStrength} ");
            Console.WriteLine($"Protocol: {stream.SslProtocol}");
        }
        private static void DisplaySecurityServices(SslStream stream)
        {
            Console.WriteLine($"Is authenticated: {stream.IsAuthenticated} as server? {stream.IsServer}");
            Console.WriteLine($"IsSigned: {stream.IsSigned}");
            Console.WriteLine($"Is Encrypted: {stream.IsEncrypted}");
        }
        private static void DisplayStreamProperties(SslStream stream)
        {
            Console.WriteLine($"Can read: {stream.CanRead}, write {stream.CanWrite}");
            Console.WriteLine($"Can timeout: {stream.CanTimeout}");
        }
        private static void DisplayCertificateInformation(SslStream stream)
        {
            Console.WriteLine($"Certificate revocation list checked: {stream.CheckCertRevocationStatus}");

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else Console.WriteLine("Local certificate is null.");

            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else Console.WriteLine("Remote certificate is null.");
        }
    }
}
