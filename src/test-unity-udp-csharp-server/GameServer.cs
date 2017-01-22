using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Threading;

namespace test_unity_udp_csharp_server
{
    public class GameServer : INetEventListener
    {
        public int port = 9050;
        public string connectionKey = "testejean";

        public readonly int messageMaxLength = 200;
        public readonly char messageTypeSeparator = '#';
        public readonly char messageValuesSeparator = '!';

        public NetServer server = null;
        public List<GameClient> clients = new List<GameClient>();

        public void Start()
        {
            server = new NetServer(this, 100, connectionKey);
            server.DiscoveryEnabled = true;
            server.DisconnectTimeout = 5 * 60 * 1000;
            server.Start(port);

            Console.WriteLine("Server started at {0}", port);

            while (!Console.KeyAvailable)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }

            server.Stop();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("[Server] Peer connected: " + peer.EndPoint);
            var peers = server.GetPeers();
            foreach (var netPeer in peers)
            {
                Console.WriteLine("ConnectedPeersList: id={0}, ep={1}", netPeer.Id, netPeer.EndPoint);
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectReason disconnectReason, int socketErrorCode)
        {
            Console.WriteLine("[Server] Peer disconnected: " + peer.EndPoint + ", reason: " + disconnectReason);
        }

        public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode)
        {
            Console.WriteLine("[Server] error: " + socketErrorCode);
        }

        public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
        {
            Console.WriteLine("[Server] ReceiveUnconnected {0}. From: {1}. Data: {2}", messageType, remoteEndPoint, reader.GetString(100));
            NetDataWriter writer = new NetDataWriter();
            writer.Put("SERVER DISCOVERY RESPONSE :)");
            server.SendDiscoveryResponse(writer, remoteEndPoint);
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {

        }

        public void OnNetworkReceive(NetPeer fromPeer, NetDataReader dataReader)
        {
            string completeMessage = dataReader.GetString(messageMaxLength);
            string[] arrMessageParts = completeMessage.Split(messageTypeSeparator);
            string messageType = arrMessageParts[0];
            string[] arrValues = arrMessageParts[1].Split(messageValuesSeparator);

            if (messageType == ToServerMessageType.USER_CONNECT.ToString("D"))
            {
                OnUserConnected(fromPeer, arrValues);
            }
            else if (messageType == ToServerMessageType.PLAY.ToString("D"))
            {
                OnUserPlay(fromPeer, arrValues);
            }
            else if (messageType == ToServerMessageType.MOVE.ToString("D"))
            {
                OnUserMove(fromPeer, arrValues);
            }
            else if (messageType == ToServerMessageType.USER_DISCONNECT.ToString("D"))
            {
                OnUserDisconnected(fromPeer, arrValues);
            }
            else
            {
                Console.WriteLine("[WARNING] Received a message with unknown type: " + completeMessage);
            }
        }

        public void OnUserConnected(NetPeer fromPeer, string[] values)
        {
            //out values:
            //0: id            //1: name            //2: x            //3: y            //4: z

            foreach (GameClient client in clients)
            {
                string message = client.id + messageValuesSeparator + client.name + messageValuesSeparator + client.x + messageValuesSeparator + client.y + messageValuesSeparator + client.z;
                SendMessage(fromPeer, FromServerMessageType.USER_CONNECTED, message);
                Console.WriteLine("User name " + client.name + " is connected..");
            }
        }

        public void OnUserPlay(NetPeer fromPeer, string[] values)
        {
            //in values:
            //0: name            //1: x            //2: y            //3: z

            GameClient currentClient = new GameClient()
            {
                id = GenerateID(),
                name = values[0],
                x = values[1],
                y = values[2],
                z = values[3],
                peer = fromPeer
            };

            clients.Add(currentClient);

            //values:
            //0: id            //1: name            //2: x            //3: y            //4: z
            string playMessage = currentClient.id + messageValuesSeparator + String.Join(messageValuesSeparator.ToString(), values);
            SendMessage(currentClient.peer, FromServerMessageType.PLAY, playMessage);

            //values:
            //0: id            //1: name            //2: x            //3: y            //4: z
            string message = playMessage;
            SendMessageToEveryoneButMe(currentClient, FromServerMessageType.USER_CONNECTED, message);
        }

        public void OnUserMove(NetPeer fromPeer, string[] values)
        {
            //in values:
            //0: id            //1: name            //2: x            //3: y            //4: z

            GameClient currentClient = clients.Where(c => c.id == values[0]).FirstOrDefault();
            if (currentClient != null)
            {
                currentClient.x = values[2];
                currentClient.y = values[3];
                currentClient.z = values[4];
            }

            var message = String.Join(messageValuesSeparator.ToString(), values);
            SendMessageToEveryoneButMe(currentClient, FromServerMessageType.MOVE, message);

            Console.WriteLine(currentClient.name + " Move to [x: " + currentClient.x + ", y: " + currentClient.y + ", z: " + currentClient.z + "]");
        }

        public void OnUserDisconnected(NetPeer fromPeer, string[] values)
        {
            //in values:
            //0: id

            GameClient currentClient = clients.Where(c => c.id == values[0]).FirstOrDefault();
            RemoveClient(currentClient);

            //out values:
            //0: name

            string message = currentClient.name;
            SendMessageToEveryoneButMe(currentClient, FromServerMessageType.USER_DISCONNECTED, message);
        }

        public void RemoveClient(GameClient client)
        {
            clients = clients.Where(c => c.id != client.id).ToList<GameClient>();
        }

        public string GenerateID()
        {
            return Guid.NewGuid().ToString("N");
        }

        public void SendMessage(NetPeer peer, FromServerMessageType type, string message)
        {
            string completeMessage = type.ToString("D") + messageTypeSeparator + message;
            NetDataWriter writer = new NetDataWriter();
            writer.Put(completeMessage);
            peer.Send(writer, SendOptions.ReliableOrdered);
        }

        public void SendMessageToEveryoneButMe(GameClient me, FromServerMessageType type, string message)
        {
            string completeMessage = type.ToString("D") + messageTypeSeparator + message;
            NetDataWriter writer = new NetDataWriter();
            writer.Put(completeMessage);

            List<GameClient> clientsToSend = clients.Where(c => c.id != me.id).ToList<GameClient>();
            foreach (GameClient client in clientsToSend)
            {
                client.peer.Send(writer, SendOptions.ReliableOrdered);
            }
        }

        public enum FromServerMessageType
        {
            USER_CONNECTED = 0,
            PLAY = 1,
            MOVE = 2,
            USER_DISCONNECTED = 3
        }

        public enum ToServerMessageType
        {
            USER_CONNECT = 0,
            PLAY = 1,
            MOVE = 2,
            USER_DISCONNECT = 3
        }
    }
}
