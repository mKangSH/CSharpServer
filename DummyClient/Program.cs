using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerCore;

namespace DummyClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            Connector connector = new Connector();
            connector.Connect(endPoint, () => { return SessionManager.Instance.Generate(); }, 
                count: 500);

            while (true)
            {
                try
                {
                    SessionManager.Instance.SendForEach();
                }

                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                // 일반적인 mmo 이동 패킷은 250ms
                Thread.Sleep(250);
            }
        }
    }
}