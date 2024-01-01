using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        // [size : 2bytes] [packetId : 2bytes]
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;

            while(true)
            {
                if(buffer.Count < HeaderSize)
                {
                    break;
                }

                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if(buffer.Count < dataSize)
                {
                    break;
                }

                // Packet 조립 가능
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
                
                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }

    public abstract class Session
    {
        Socket _socket;
        int _disconnected = 0;

        RecvBuffer _recvBuffer = new RecvBuffer(1024);

        object _lock = new object();
        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnDisconnected(EndPoint endPoint);
        public abstract void OnSend(int numOfBytes);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        
        private void Clear()
        {
            lock(_lock)
            {
                _sendQueue.Clear();
                _pendingList.Clear();
            }
        }

        public void Start(Socket socket)
        {
            _socket = socket;

            _recvArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

            _sendArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0)
                {
                    RegisterSend();
                }
            }
        }

        public void Disconnect()
        {
            if(Interlocked.Exchange(ref _disconnected, 1) == 1)
            {
                return;
            }

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();

            Clear();
        }

        #region 네트워크 통신

        void RegisterSend()
        {
            if(_disconnected == 1)
            {
                return;
            }

            while(_sendQueue.Count > 0)
            {
                ArraySegment<byte> buff = _sendQueue.Dequeue();
                _pendingList.Add(buff);
            }

            _sendArgs.BufferList = _pendingList;

            try
            {
                bool pending = _socket.SendAsync(_sendArgs);
                if (pending == false)
                {
                    OnSendCompleted(null, _sendArgs);
                }
            }
            
            catch (Exception ex)
            {
                Console.WriteLine($"Register Send Failed : {ex.Message}");
            }
        }

        void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)
                        {
                            RegisterSend();
                        }
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {ex}");
                    }
                }

                else
                {
                    Disconnect();
                }
            }
        }

        void RegisterRecv()
        {
            if(_disconnected == 1)
            {
                return;
            }

            _recvBuffer.Clean();

            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            try
            {
                bool pending = _socket.ReceiveAsync(_recvArgs);
                if (pending == false)
                {
                    OnRecvCompleted(null, _recvArgs);
                }
            }

            catch(Exception ex) 
            {
                Console.WriteLine($"Register Recv Failed : {ex.Message}");
            }
        }

        void OnRecvCompleted(object? sender, SocketAsyncEventArgs args) 
        {
            if(args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // Write cursor
                    if(_recvBuffer.OnWrite(args.BytesTransferred) == false) 
                    {
                        Disconnect();
                        return;
                    }

                    // Contents
                    int processLen = OnRecv(_recvBuffer.ReadSegment);
                    if(processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        Disconnect();
                        return;
                    }

                    if(_recvBuffer.OnRead(processLen) == false)
                    {
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                }

                catch(Exception ex) 
                {
                    Console.WriteLine($"OnRecvCompleted Failed {ex}");
                }
            }

            else
            {
                Disconnect();
            }
        }

        #endregion
    }
}
