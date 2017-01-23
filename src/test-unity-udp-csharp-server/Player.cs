﻿using LiteNetLib;
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
        public float x;
        public float y;
        public float z;
        [JsonIgnore]
        public NetPeer peer;

        public Player(string name)
        {
            this.name = name;
            this.id = GenerateID();
            this.maxHP = 100;
            this.currentHP = maxHP;
            this.x = 0f;
            this.y = 0f;
            this.z = 0f;
        }

        public string GenerateID()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
