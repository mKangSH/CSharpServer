using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class GameRoom : IJobQueue
    {
        // Dictionary <id, session>
        List<ClientSession> _sessions = new List<ClientSession>();
        JobQueue _jobQueue = new JobQueue();

        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        public void Flush()
        {
            foreach(ClientSession s in _sessions)
            {
                s.Send(_pendingList);
            }

            //Console.WriteLine($"Flushed {_pendingList.Count} items");
            _pendingList.Clear();
        }

        public void Broadcast(ArraySegment<byte> segment)
        {
            _pendingList.Add(segment);
        }

        public void Enter(ClientSession session)
        {
            // Add new Player
            _sessions.Add(session);
            session.Room = this;

            // Send all player list to new player
            S_PlayerList playersPacket = new S_PlayerList();
            foreach(ClientSession s in _sessions)
            {
                playersPacket.players.Add(new S_PlayerList.Player()
                {
                    isSelf = (s == session),
                    playerId = s.SessionId,
                    posX = s.PosX,
                    posY = s.PosY,
                    posZ = s.PosZ,
                });
            }

            session.Send(playersPacket.Write());

            // Send all player to enter new player 
            S_BroadcastEnterGame enter = new S_BroadcastEnterGame();
            enter.playerId = session.SessionId;
            enter.posX = 0;
            enter.posY = 0;
            enter.posZ = 0;
            Broadcast(enter.Write());
        }

        public void Exit(ClientSession session)
        {
            // Remove player
            _sessions.Remove(session);

            // Send all player to exit player
            S_BroadcastExitGame exit = new S_BroadcastExitGame();
            exit.playerId = session.SessionId;
            Broadcast(exit.Write());
        }

        public void Push(Action job)
        {
            _jobQueue.Push(job);
        }

        public void Move(ClientSession session, C_Move packet)
        {
            // Change position
            session.PosX = packet.posX;
            session.PosY = packet.posY;
            session.PosZ = packet.posZ;

            // Send all player to move player
            S_BroadcastMove move = new S_BroadcastMove();
            move.playerId = session.SessionId;
            move.posX = packet.posX;
            move.posY = packet.posY;
            move.posZ = packet.posZ;
            Broadcast(move.Write());
        }
    }
}
