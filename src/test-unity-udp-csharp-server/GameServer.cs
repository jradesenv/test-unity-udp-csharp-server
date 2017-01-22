﻿using System;
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
        public int saveEveryXMilliseconds = 5 * 60 * 1000;
        public DateTime lastSaveTime = new DateTime();

        public readonly int messageMaxLength = 200;
        public readonly char messageTypeSeparator = '#';
        public readonly char messageValuesSeparator = '!';

        public NetServer server = null;
        public List<Player> playersOnline = new List<Player>();
        public List<GameAccount> accounts = new List<GameAccount>();

        public void Start()
        {
            accounts = Repository.LoadGameAccounts();

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

        public void SaveGameState()
        {
            if(lastSaveTime.AddMilliseconds(saveEveryXMilliseconds) > DateTime.UtcNow)
            {
                lastSaveTime = DateTime.UtcNow;
                Repository.SaveGameAccounts(accounts);
            }
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

            if (messageType == ToServerMessageType.CREATE_ACCOUNT.ToString("D"))
            {
                OnCreateAccount(fromPeer, arrValues);
            }
            if (messageType == ToServerMessageType.LOGIN.ToString("D"))
            {
                OnLogin(fromPeer, arrValues);
            }
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
            else if (messageType == ToServerMessageType.GET_USER.ToString("D"))
            {
                OnGetUser(fromPeer, arrValues);
            }
            else
            {
                Console.WriteLine("[WARNING] Received a message with unknown type: " + completeMessage);
            }

            SaveGameState();
        }

        public void OnCreateAccount(NetPeer fromPeer, string[] values)
        {
            //in values:
            //0: username            //1: password            //2: name

            GameAccount old = accounts.Where(a => a.username == values[0]).FirstOrDefault();
            if(old == null)
            {
                GameAccount newAccount = new GameAccount()
                {
                    username = values[0],
                    password = values[1],
                    player = new Player(values[2])
                };
                accounts.Add(newAccount);

                string message = newAccount.player.id + 
                SendMessage(fromPeer, FromServerMessageType.USER_ALREADY_EXISTS, message);
            } else
            {
                string message = "username already exists!";
                SendMessage(fromPeer, FromServerMessageType.USER_ALREADY_EXISTS, message);
            }
        }

        public void OnLogin(NetPeer fromPeer, string[] values)
        {

        }

        public void OnUserConnected(NetPeer fromPeer, string[] values)
        {
            //out values:
            //0: id            //1: name            //2: x            //3: y            //4: z

            foreach (Player player in playersOnline)
            {
                string message = player.id + messageValuesSeparator + player.name + messageValuesSeparator + player.x + messageValuesSeparator + player.y + messageValuesSeparator + player.z;
                SendMessage(fromPeer, FromServerMessageType.USER_CONNECTED, message);
                Console.WriteLine("User name " + player.name + " is connected..");
            }
        }

        public void OnGetUser(NetPeer fromPeer, string[] values)
        {
            //in values:
            //0: id 
            Console.WriteLine("Someone asked for user: " + values[0]);
            Player player = playersOnline.Where(c => c.id == values[0]).FirstOrDefault();
            if(player != null)
            {
                string message = player.id + messageValuesSeparator + player.name + messageValuesSeparator + player.x + messageValuesSeparator + player.y + messageValuesSeparator + player.z;
                SendMessage(fromPeer, FromServerMessageType.USER_CONNECTED, message);
            }
        }

        public void OnUserPlay(NetPeer fromPeer, string[] values)
        {
            ////in values:
            ////0: name            //1: x            //2: y            //3: z

            //Player currentPlayer = new Player()
            //{
            //    id = GenerateID(),
            //    name = values[0],
            //    x = values[1],
            //    y = values[2],
            //    z = values[3],
            //    peer = fromPeer
            //};

            //playersOnline.Add(currentPlayer);

            ////values:
            ////0: id            //1: name            //2: x            //3: y            //4: z
            //string playMessage = currentPlayer.id + messageValuesSeparator + String.Join(messageValuesSeparator.ToString(), values);
            //SendMessage(currentPlayer.peer, FromServerMessageType.PLAY, playMessage);

            ////values:
            ////0: id            //1: name            //2: x            //3: y            //4: z
            //string message = playMessage;
            //SendMessageToEveryoneButMe(currentPlayer, FromServerMessageType.USER_CONNECTED, message);
        }

        public void OnUserMove(NetPeer fromPeer, string[] values)
        {
            //in values:
            //0: id            //1: name            //2: x            //3: y            //4: z

            Player currentPlayer = playersOnline.Where(c => c.id == values[0]).FirstOrDefault();
            if (currentPlayer != null)
            {
                currentPlayer.x = values[2];
                currentPlayer.y = values[3];
                currentPlayer.z = values[4];
            }

            var message = String.Join(messageValuesSeparator.ToString(), values);
            SendMessageToEveryoneButMe(currentPlayer, FromServerMessageType.MOVE, message);

            Console.WriteLine(currentPlayer.name + " Move to [x: " + currentPlayer.x + ", y: " + currentPlayer.y + ", z: " + currentPlayer.z + "]");
        }

        public void OnUserDisconnected(NetPeer fromPeer, string[] values)
        {
            //in values:
            //0: id

            Player currentPlayer = playersOnline.Where(c => c.id == values[0]).FirstOrDefault();
            RemoveClient(currentPlayer);

            //out values:
            //0: name

            string message = currentPlayer.name;
            SendMessageToEveryoneButMe(currentPlayer, FromServerMessageType.USER_DISCONNECTED, message);
        }

        public void RemoveClient(Player client)
        {
            playersOnline = playersOnline.Where(c => c.id != client.id).ToList<Player>();
        }

        public void SendMessage(NetPeer peer, FromServerMessageType type, string message)
        {
            string completeMessage = type.ToString("D") + messageTypeSeparator + message;
            NetDataWriter writer = new NetDataWriter();
            writer.Put(completeMessage);
            peer.Send(writer, SendOptions.ReliableOrdered);
        }

        public void SendMessageToEveryoneButMe(Player me, FromServerMessageType type, string message)
        {
            string completeMessage = FormatMessageContent(type, message);
            NetDataWriter writer = new NetDataWriter();
            writer.Put(completeMessage);

            List<Player> playersToSend = playersOnline.Where(c => c.id != me.id).ToList<Player>();
            foreach (Player client in playersToSend)
            {
                client.peer.Send(writer, SendOptions.ReliableOrdered);
            }
        }

        public string FormatMessageContent(FromServerMessageType type, params string[] args)
        {
            string message = type.ToString("D") + messageTypeSeparator;
            int argsCount = args.Count();

            if(argsCount == 1)
            {
                message = message + args[0];
            } else if (argsCount > 1)
            {
                message = message + String.Join(messageValuesSeparator.ToString(), args);
            }

            return message;
        }

        public enum FromServerMessageType
        {
            USER_ALREADY_EXISTS = 0,
            USER_CONNECTED = 0,
            PLAY = 1,
            MOVE = 2,
            USER_DISCONNECTED = 3
        }

        public enum ToServerMessageType
        {
            LOGIN = 0,
            CREATE_ACCOUNT = 1,
            USER_CONNECT = 2,
            PLAY = 3,
            MOVE = 4,
            USER_DISCONNECT = 5,
            GET_USER = 6
        }
    }
}
