using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    internal class Listener
    {
        Socket _listenerSocket;
        Action<Socket> _onAcceptHandler;

        public Listener(IPEndPoint endPoint, Action<Socket> onAcceptHandler)
        {
            _onAcceptHandler += onAcceptHandler;
            _listenerSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind
            _listenerSocket.Bind(endPoint);

            // 최대 대기수(10)
            _listenerSocket.Listen(10);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args);
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;

            bool pending = _listenerSocket.AcceptAsync(args);
            if (pending == false)
            {
                OnAcceptCompleted(null, args);
            }
        }

        void OnAcceptCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success)
            {
                // TODO : 
                _onAcceptHandler.Invoke(args.AcceptSocket);
            }

            else
            {
                Console.WriteLine(args.SocketError.ToString());
            }

            RegisterAccept(args);
        }

        public Socket Accept()
        {
            _listenerSocket.AcceptAsync();
            return _listenerSocket.Accept();
        }
    }
}
