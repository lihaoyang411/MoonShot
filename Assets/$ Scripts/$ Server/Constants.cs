using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace TeamBlack.MoonShot
{
    public static class Constants
    {
        public static class Networking
        {
            public static readonly IPAddress LOCAL_HOST = new IPAddress(new byte[] { 127, 0, 0, 1 });
            public const int TCP_PORT = 8081;
            public const int READ_BUFF_SIZE = 80000;
        }

        public static class Global
        {
            public const int MAX_ENTITIES = 30000;
            public const int ORE_SELL_VALUE = 5;
            public const int START_CREDITS = 0;
            public const double INCOME_INTERVAL = 3;
            public const int INCOME_VALUE = 1;
        }

        public static class Entities
        {
            // Everything byte <= UNIT_IDS corresponds to a unit
            public const byte UNIT_IDS = 20;

            public enum Status : byte
            {
                Idle,
                Moving,
                Working,
                Interacting,
                Deployed
            };

            // Hub 255

            public static class Hub
            {
                // Core Stats
                public const  byte  ID              = 255;
                public static byte  VISION          = 15;
                public static int   HEALTH          = 30000;
                public static byte  INVENTORY       = 200;
            }

            //Deployables <= 254

            public static class ProximityMine
            {
                // Core Stats
                public const  byte  ID              = 254;
                public static byte  VISION          = 3;
                public static int   HEALTH          = 3;
                public static bool  CLOAKED         = true;

                public static float FUSE_TIME       = 0.4f;
                public static int   ATTACK_DAMAGE   = 900;
                public static byte  ATTACK_RANGE    = 3;
            }

            public static class TimeBomb
            {
                // Core Stats
                public const  byte  ID              = 253;
                public static byte  VISION          = 3;
                public static int   HEALTH          = 3;

                public static float FUSE_TIME       = 5f;
                public static int   ATTACK_DAMAGE   = 1800;
                public static byte  ATTACK_RANGE    = 6;
            }

            public static class QuickSand
            {
                // Core Stats
                public const  byte  ID              = 252;
                public static byte  VISION          = 3;
                public static int   HEALTH          = 3;

                public static float FUSE_TIME       = 5f;
                public static byte  ATTACK_RANGE    = 2;
            }

            public static class GasBomb
            {
                // Core Stats
                public const byte ID = 251;
                public static byte VISION = 15;
                public static int HEALTH = 500;

                public static int ATTACK_DAMAGE = 1;
                public static float ATTACK_INTERVAL = 0.2f;

                //public static float EMISSION_FACTOR = 1000f;
                public static float DECAY_FACTOR = 0.01f;
                //public static float GAS_MAX = 1000f;
            }

            public static class HealStation
            {
                // Core Stats
                public const byte ID = 250;
                public static byte VISION = 6;
                public static int HEALTH = 500;

                public static int HEAL_RADIUS = 3;
                public static int HEAL_AMOUNT = 15;
                public static float HEAL_INTERVAL = 0.5f;
            }

            public static class SonicBomb
            {
                // Core Stats
                public const byte ID = 249;
                public static byte VISION = 3;
                public static int HEALTH = 100;

                public static int REVEAL_RADIUS = 15;
                public static float CHARGE_TIME = 3;
            }

            public static class AntimatterBomb
            {
                // Core Stats
                public const byte ID = 248;
                public static byte VISION = 5;
                public static int HEALTH = 1000;

                public static float FUSE_TIME = 30f;
                public static int ATTACK_DAMAGE = 1500;
                public static byte ATTACK_RANGE = 15;
            }

            public static class SandBags
            {
                // Core Stats
                public const byte ID = 247;
                public static byte VISION = 0;
                public static int HEALTH = 1500;
            }

            // Resources <= 155

            public static class Ore
            {
                // Core Stats
                public const  byte  ID              = 155;
                public static int   HEALTH          = 1;
            }

            // Units >= 0

            public static class Digger
            {
                // Core Stats
                public const  byte  ID              = 0;
                public static byte  VISION          = 7;
                public static int   HEALTH          = 700;
                public static float SPEED           = 4f;
                public static byte  INVENTORY       = 1;

                // Combat Stats
                public static int   ATTACK_DAMAGE   = 100;
                public static float ATTACK_INTERVAL = 1f; // used for attacking and block breaking
                public static byte  ATTACK_RANGE    = 1;
            }
            public static class Miner
            {
                // Core Stats
                public const  byte  ID              = 1;
                public static byte  VISION          = 7;
                public static int   HEALTH          = 1000;
                public static float SPEED           = 3f;
                public static byte  INVENTORY       = 2;

                // Combat Stats
                public static int   ATTACK_DAMAGE   = 200;
                public static float ATTACK_INTERVAL = 4f; // used for attacking and block breaking
                public static byte  ATTACK_RANGE    = 1;
            }
            public static class Soldier
            {
                // Core Stats
                public const  byte  ID              = 2;
                public static byte  VISION          = 12;
                public static int   HEALTH          = 2000;
                public static float SPEED           = 4f;
                public static byte  INVENTORY       = 2;

                // Combat Stats
                public static int   ATTACK_DAMAGE   = 30;
                public static float ATTACK_INTERVAL = 0.1f; 
                public static byte  ATTACK_RANGE    = 6;
            }
            public static class Hauler
            {
                // Core Stats
                public const  byte  ID              = 3;
                public static byte  VISION          = 12;
                public static int   HEALTH          = 2000;
                public static float SPEED           = 6f;
                public static byte  INVENTORY       = 8;

                // Combat Stats
                public static int   ATTACK_DAMAGE   = 300;
                public static float ATTACK_INTERVAL = 10f; 
                public static byte  ATTACK_RANGE    = 1;
            }
        }
    }
}
