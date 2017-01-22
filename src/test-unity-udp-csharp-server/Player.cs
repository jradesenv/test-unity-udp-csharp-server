using LiteNetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace test_unity_udp_csharp_server
{
    public class Player
    {
        public string id;
        public string name;
        public int currentHP;
        public int maxHP;
        public string x;
        public string y;
        public string z;
        [JsonIgnore]
        public NetPeer peer;

        public Player(string name)
        {
            this.name = name;
            this.id = GenerateID();
            this.maxHP = 100;
            this.currentHP = maxHP;
            this.x = 0f.ToString();
            this.y = 0f.ToString();
            this.z = 0f.ToString();
        }

        public string GenerateID()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
