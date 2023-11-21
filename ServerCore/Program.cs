using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    internal class Program
    {
        static Listener? _listener;

        static void OnAccepthandler(Socket clientSocket)
        {
            try
            {
                // Recv
                byte[] recvBuff = new byte[1024];
                int recvBytes = clientSocket.Receive(recvBuff);
                string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                Console.WriteLine($"[FromClient] {recvData}");

                // Send
                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome To MMORPG Server");
                clientSocket.Send(sendBuff);

                // Close
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            
            catch(Exception ex) 
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            _listener = new Listener(endPoint, OnAccepthandler);
            Console.WriteLine("Listening...");
            
            while (true)
            {
                ;
            }
        }
    }
}