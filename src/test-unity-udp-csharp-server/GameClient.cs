using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace test_unity_udp_csharp_server
{
    public class GameClient
    {
        public string id;
        public string name;
        public string x;
        public string y;
        public string z;
        public NetPeer peer;
    }
}
