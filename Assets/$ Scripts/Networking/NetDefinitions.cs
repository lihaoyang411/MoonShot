using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;
using JetBrains.Annotations;


namespace TeamBlack.MoonShot.Networking
{
    // Used for tracking size efficient vectors
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct ByteVector2
    {
        public static explicit operator Vector2(ByteVector2 b) => new Vector2(b.X, b.Y);
        public static explicit operator Vector2Int(ByteVector2 b) => new Vector2Int(b.X, b.Y);
        public static explicit operator ByteVector2(Vector2 b) => new ByteVector2((byte)b.x, (byte)b.y);
        public static explicit operator ByteVector2(Vector2Int b) => new ByteVector2((byte)b.x, (byte)b.y);

        public static double Distance(ByteVector2 v1, ByteVector2 v2) 
        {
            return Math.Sqrt((Math.Pow(((double)v2.X - v1.X), 2) + Math.Pow(((double)v2.Y - v1.Y), 2)));
        }

        public static int ManhattanDistance(ByteVector2 v1, ByteVector2 v2) 
        {
            return (int)Math.Abs(v1.X - v2.X) + (int)Math.Abs(v1.Y - v2.Y);
        }

        public byte X;
        public byte Y;

        public ByteVector2(byte x, byte y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(ByteVector2 left, ByteVector2 right)
        {
            return (left.X == right.X
                 && left.Y == right.Y);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(ByteVector2))
                return false;

            ByteVector2 other = (ByteVector2)obj;

            return (this.X == other.X
                 && this.Y == other.Y);
        }

        public override int GetHashCode()
        {
            return X ^ Y; // lol
        }

        public static bool operator !=(ByteVector2 left, ByteVector2 right)
        {
            return (!(left == right));
        }
        public static ByteVector2 operator +(ByteVector2 left, ByteVector2 right)
        {
            return new ByteVector2((byte)(left.X + right.X), (byte)(left.Y + right.Y));
        }

        public static ByteVector2 operator -(ByteVector2 left, ByteVector2 right)
        {
            return new ByteVector2((byte)(left.X - right.X), (byte)(left.Y - right.Y));
        }
    }

    public static class NewPackets
    {


        /* ---- Request Packets ---- */

        // These packets are sent by player clients to try and update the server game state
        // Implicitly, each of these packets has an owning faction or player (appended faction header)

        // Sent by a faction to purchase units
        public struct Buy
        {
            public byte type;

            public Buy(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                type = reader.ReadByte();

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.Write(type);

                return stream.ToArray();
            }
        }

        // For intricate inventory management
        public struct Deploy
        {
            public byte InventoryID;
            public int EntityID;

            public Deploy(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                InventoryID = reader.ReadByte();
                EntityID = reader.ReadInt32();

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.Write(InventoryID);
                writer.Write(EntityID);

                return stream.ToArray();
            }
        }

        // Tries to make the specified entity `move to` or `mine` the specified tile
        public struct TileInteraction
        {
            public int entityIndex;
            public byte tileX;
            public byte tileY;

            public TileInteraction(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                entityIndex = reader.ReadInt32();
                tileX = reader.ReadByte();
                tileY = reader.ReadByte();

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.Write(entityIndex);
                writer.Write(tileX);
                writer.Write(tileY);

                return stream.ToArray();
            }
        }

        // Tries to make the specified entity `grab` or `combat` an entity of another faction
        public struct EntityInteraction
        {
            public int myEntityIndex;
            public byte otherFactionIndex;
            public int otherEntityIndex;

            public EntityInteraction(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                myEntityIndex = reader.ReadInt32();
                otherFactionIndex = reader.ReadByte();
                otherEntityIndex = reader.ReadInt32();

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.Write(myEntityIndex);
                writer.Write(otherFactionIndex);
                writer.Write(otherEntityIndex);

                return stream.ToArray();
            }
        }

        
        /* ---- Response Packets ---- */

        // Reflects a current move job on the server that is moving a unit between tiles
        public struct MoveEntity
        {
            public byte unitIndex;

            public byte fromX;
            public byte fromY;

            public byte toX;
            public byte toY;

            public MoveEntity(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                unitIndex = reader.ReadByte();

                fromX =     reader.ReadByte();
                fromY =     reader.ReadByte();

                toX =       reader.ReadByte();
                toY =       reader.ReadByte();

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.Write(unitIndex);

                writer.Write(fromX);
                writer.Write(fromY);

                writer.Write(toX);
                writer.Write(toY);

                return stream.ToArray();
            }
        }
        
        // Reflects a change to the game server tile map
        public struct TileUpdate 
        {
            public byte tileX;
            public byte tileY;
            public byte type;

            public TileUpdate(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                tileX = reader.ReadByte();
                tileY = reader.ReadByte();
                type = reader.ReadByte();

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.Write(tileX);
                writer.Write(tileY);
                writer.Write(type);

                return stream.ToArray();

                //TODO: dispose??
            }
        }


        public struct ChunkUpdate
        {
            public struct ChunkMember
            {
                public byte x;
                public byte y;
                public Tile value;

                public ChunkMember(byte x, byte y, Tile value)
                {
                    this.x = x;
                    this.y = y;
                    this.value = value;
                }
            }

            public List<ChunkMember> ChunkMembers;

            public ChunkUpdate(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                int length = reader.ReadInt32();

                ChunkMembers = new List<ChunkMember>();

                for (int i = 0; i < length; i++)
                {
                    ChunkMembers.Add(
                        new ChunkMember(
                            reader.ReadByte(),
                            reader.ReadByte(),
                            (Tile)reader.ReadByte()
                        ));
                }
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.Write(ChunkMembers.Count);

                foreach (ChunkMember c in ChunkMembers)
                {
                    writer.Write(c.x);
                    writer.Write(c.y);
                    writer.Write((byte)c.value);
                }

                return stream.ToArray();
            }
        }

        //
        public struct MapUpdate
        {
            public byte mapSize;
            public Tile[,] map;

            public MapUpdate(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                mapSize = reader.ReadByte();
                map = new Tile[mapSize, mapSize];

                Debug.Log("READ MAP SIZE: " + mapSize);

                // Read n x n tiles from buffer
                for (int r = 0; r < mapSize; r++)
                    for (int c = 0; c < mapSize; c++)
                        map[r, c] = (Tile)reader.ReadByte();

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.Write(mapSize);
                for (int r = 0; r < mapSize; r++)
                    for (int c = 0; c < mapSize; c++)
                        writer.Write((byte)map[r, c]);

                return stream.ToArray();

                //TODO: dispose??
            }
        }


        // Reflects a change to an entity in a faction
        // Contains all possible stats for any non-tile object
        public struct EntityUpdate 
        {
            // Basic
            public int unitIndex;
            public byte unitType;
            public bool hidden;     // can the player see the entity?
            public bool dead;       // play death animation?
            public Constants.Entities.Status status;

            // Movement
            public ByteVector2 position;
            public double moveSpeed;

            // Combat
            public int health;
            public int healthCapacity;

            public ByteVector2 attackTarget;
            public bool attacking;
            public int attackDamage;
            public byte attackRange;

            // Inventory
            public byte carryCapacity;  // How much can it hold?
            public List<Byte> inventory;

            public EntityUpdate(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                // read basic
                unitIndex       = reader.ReadInt32();
                unitType        = reader.ReadByte();
                hidden          = reader.ReadBoolean();
                dead            = reader.ReadBoolean();
                status          = (Constants.Entities.Status)reader.ReadByte();

                // read movement
                position        = new ByteVector2(reader.ReadByte(), reader.ReadByte());
                moveSpeed       = reader.ReadDouble();

                // read combat
                health          = reader.ReadInt32();
                healthCapacity  = reader.ReadInt32();

                attackTarget    = new ByteVector2(reader.ReadByte(), reader.ReadByte());
                attacking       = reader.ReadBoolean();
                attackDamage    = reader.ReadInt32();
                attackRange     = reader.ReadByte();

                // read inventory
                carryCapacity   = reader.ReadByte();
                int carried = reader.ReadByte();
                inventory = new List<byte>();
                for (int i = 0; i < carried; i++)
                {
                    inventory.Add(reader.ReadByte());
                }

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                if(inventory == null)
                    inventory = new List<byte>();

                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                // write basic
                writer.Write(unitIndex);
                writer.Write(unitType);
                writer.Write(hidden);
                writer.Write(dead);
                writer.Write((byte)status);

                // write movement
                writer.Write(position.X);
                writer.Write(position.Y);
                writer.Write(moveSpeed);

                // write combat
                writer.Write(health);
                writer.Write(healthCapacity);

                writer.Write(attackTarget.X);
                writer.Write(attackTarget.Y);
                writer.Write(attacking);
                writer.Write(attackDamage);
                writer.Write(attackRange);

                // write inventory
                writer.Write(carryCapacity);
                writer.Write((byte)inventory.Count);
                for (int i = 0; i < inventory.Count; i++)
                {
                    writer.Write(inventory[i]);
                }

                return stream.ToArray();
            }
        }

        // Reflects a change to a faction i.e. player client
        // Right now the id is defined by a header
        public struct FactionUpdate
        {
            public bool connected;
            public Int32 credits;

            public FactionUpdate(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                connected = true;
                credits = reader.ReadInt32();

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                //writer.Write(connected);
                writer.Write(credits);

                //var reader = new BinaryReader(new MemoryStream(stream.ToArray()));
                //Debug.Log("SERI: PRE READING... " + reader.ReadInt32());

                return stream.ToArray();
            }
        }

        public struct GameOver
        {
            public int WinningFaction;

            public GameOver(byte[] fromByteArray)
            {
                var reader = new BinaryReader(new MemoryStream(fromByteArray));

                WinningFaction = reader.ReadInt32();

                reader.Dispose();
            }

            public byte[] ByteArray()
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.Write(WinningFaction);

                return stream.ToArray();
            }
        }

        /* ---- Header Macros ---- */

        // Appends packet type designator header
        public static byte[] AppendTypeHeader(PacketType type, byte[] packet)
        {
            return AppendByteHeader((byte)type, packet);
        }

        // Appends 1 byte to the front of the packet 
        // For faction headers
        public static byte[] AppendByteHeader(byte header, byte[] packet)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(new byte[]{header}, 0, 1); // lol
            stream.Write(packet, 0, packet.Length);

            return stream.ToArray();
        }

        public static byte[] PopTypeHeader(byte[] packet, out PacketType header)
        {
            byte byteHeader;
            byte[] toReturn = PopByteHeader(packet, out byteHeader);
            header = (PacketType)byteHeader;
            return toReturn;
        }

        public static byte[] PopByteHeader(byte[] packet, out byte header)
        {
            var reader = new BinaryReader(new MemoryStream(packet));
            header = reader.ReadByte();
            reader.Dispose();

            // Return packet without header
            byte[] toReturn = new byte[packet.Length-1];
            Array.Copy(packet, 1, toReturn, 0, toReturn.Length);
            return toReturn;
        }


        /* ---- Packet Types ---- */

        public enum PacketType : byte
        {
            // Invalid
            Null,

            // For game start (client connect responses)
            InitSelf,
            InitOther,
            
            // Request entity or tile interaction
            EntityInteractRequest,
            TileInteractRequest,
            Buy,
            Deploy,

            // Entity responses
            MoveEntity,
            UpdateEntity,
            // ASAP TODO: expand these responses
            // - SpawnEntity (init, buying, ore drops)
            // - UpdateEntityState (building, carrying, dropping, resurrecting)
            // - UpdateEntityInventory
            

            // Faction responses
            UpdateFaction,

            // Tile map responses
            TileUpdate,
            MapUpdate,
            ChunkUpdate,
            GameOver
        };
    }

    //public class Constants
    //{
    //    public static readonly IPAddress LOCAL_HOST = new IPAddress(new byte[] { 127, 0, 0, 1 });
    //    public const int TCP_PORT = 8081;
    //    public const int READ_BUFF_SIZE = 80000;

    //    // prevent instantiation
    //    private Constants()
    //    {
    //    }
    //}

    public static class Packets
    {
        public static byte[] Serialize<T>(T data)
        where T : struct
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, data);
            return stream.ToArray();
        }
        public static T Deserialize<T>(byte[] array)
            where T : struct
        {
            var stream = new MemoryStream(array);
            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }

        public static void DeserializeWithPlayer<T>(byte[] array, out T packet, out int player)
        where T : struct 
        {
            var stream = new MemoryStream(array);
            var formatter = new BinaryFormatter();
                
            var factionHeader = (FactionHeader)formatter.Deserialize(stream);
            player = factionHeader.id;        

            packet = (T)formatter.Deserialize(stream);
        }


        public static byte[] SerializePacket<T>(Packets.PacketType type, T packet)
            where T : struct
        {
            Packets.TypeHeader header;
            header.type = type;
            byte[] headerPacket = Packets.Serialize(header);
            byte[] bodyPacket = Packets.Serialize(packet);

            MemoryStream stream = new MemoryStream();
            stream.Write(headerPacket, 0, headerPacket.Length);
            stream.Write(bodyPacket, 0, bodyPacket.Length);
            return stream.ToArray();
        }

        public static byte[] SerializePlayerPacket<T>(int factionID, PacketType type, T packet)
            where T : struct
        {
            // Make faction packet
            FactionHeader fHeader;
            fHeader.id = factionID;

            // Make header packet
            Packets.TypeHeader header;
            header.type = type;

            // Convert packets to bytes
            byte[] factionPacket = Packets.Serialize(fHeader);
            byte[] headerPacket = Packets.Serialize(header);
            byte[] bodyPacket = Packets.Serialize(packet);

            // Merge packets and return
            MemoryStream stream = new MemoryStream();
            stream.Write(headerPacket, 0, headerPacket.Length);
            stream.Write(factionPacket, 0, factionPacket.Length);
            stream.Write(bodyPacket, 0, bodyPacket.Length);


            Debug.Log("SERIALIZATION DONE.");

            return stream.ToArray();
        }

        public enum PacketType : byte
        {
            Null,
            Init,
            BlockUpdate,
            Move, RequestMove,
            Mine, RequestMine,
            UnitUpdatePacket,
            Map,
            InitSelf, InitPlayer,
            MyCredits,
            Etc
        };

        // FIXME: ensure this is right and that we are packing our packets correctly, maybe packing 
        //        can be used
        // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.structlayoutattribute.pack?view=netframework-4.8
        // https://stackoverflow.com/questions/17105504/why-sizeof-of-a-struct-is-unsafe
        // [StructLayout(LayoutKind.Explicit, Size = 1, Pack = 1)]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct FactionHeader
        {
            public int id;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct TypeHeader
        {
            public PacketType type;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct SizeHeader
        {
            public int size;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct RequestMove
        {
            public byte unitIndex;
            public ByteVector2 from;
            public ByteVector2 to;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct RequestMine
        {
            public byte unitIndex;
            public ByteVector2 position;
        }

        // Better response for movement/pathfinding
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct Move
        {
            // The unit to move
                                              public byte unitIndex;
            // Every two bytes is a waypoint
            public ByteVector2 waypoint;
            // Fix Me: add previous waypoint for correction
        }

        // Response for game start
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct Init
        {
            byte[,] map;
        }

        // Sent to initialize a player's own faction or an opposing faction
        // TODO: in the future will only initialize own faction
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct InitFaction
        {
            // Location of the player's base
            public ByteVector2 hubLocation;

            // All starting units are miners
            // miner IDs are the indices
            public ByteVector2[] unitLocations;
        }

        // Response for mining        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct BlockUpdate
        {
            public Tile NewTile;
            public ByteVector2 Position;

            public BlockUpdate(Tile t, ByteVector2 position)
            {
                NewTile = t;
                Position = position;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct Map
        {
            public Tile[,] map;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct MyCredits
        {
            public int credits;
        }

        // PACKET HEADER STRUCT ^^^
        // https://www.genericgamedev.com/general/converting-between-structs-and-byte-arrays/
    }
}
