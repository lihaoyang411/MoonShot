using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections.Concurrent;

namespace TeamBlack.MoonShot.Networking
{
    public class ServerGameManager : IDisposable
    {

        public const int FixedTimeStep = 20;
        public double DeltaTime = 0f;
        private double IncomeTimer = 0;

        public ServerMapManager MapManager;
        public List<ServerFactionManager> PlayerFactions;

        public System.Threading.Timer GameClock;
        System.Diagnostics.Stopwatch GameWatch;

        public Server GameServer;

        public System.Random NumberGenerator;

        private ConcurrentQueue<byte[]> _newPackets;


        public ServerGameManager()
        {
            // Game init
            NumberGenerator = new System.Random();
            MapManager = new ServerMapManager(100, this);
            PlayerFactions = new List<ServerFactionManager>();


            // Server init
            _newPackets = new ConcurrentQueue<byte[]>();
            GameServer = new Server();
            GameServer.Start(RecievePacket, AddPlayer);

            // Add a new "player manager" for managing ore and other neutral entities
            // The neutral faction is assumed to be index 0
            PlayerFactions.Add(new ServerFactionManager(
                PlayerFactions.Count, //id
                MapManager,           //context
                this));

            // Start game loop
            StartGameClock();
        }

        // New API

        public void Send(byte fromFaction, NewPackets.PacketType ofType, byte[] packet, ConnectedClient client)
        {
            packet = NewPackets.AppendByteHeader(fromFaction, packet);
            packet = NewPackets.AppendTypeHeader(ofType, packet);

            countBytes += packet.Length;

            GameServer.Send(client, packet);
        }

        public void Send(NewPackets.PacketType ofType, byte[] packet, ConnectedClient client)
        {
            packet = NewPackets.AppendTypeHeader(ofType, packet);

            countBytes += packet.Length;

            GameServer.Send(client, packet);
        }

        int countBytes = 0;
        public void Broadcast(byte fromFaction, NewPackets.PacketType ofType, byte[] packet)
        {
            packet = NewPackets.AppendByteHeader(fromFaction, packet);
            packet = NewPackets.AppendTypeHeader(ofType, packet);

            countBytes += packet.Length;

            GameServer.Broadcast(packet);
        }

        // For generic packets like tile updates
        public void Broadcast(NewPackets.PacketType ofType, byte[] packet)
        {
            packet = NewPackets.AppendTypeHeader(ofType, packet);

            countBytes += packet.Length;

            GameServer.Broadcast(packet);
        }

        private void RecievePacket(byte[] packetBuffer, int bytesToRead)
        {
            if (packetBuffer != null && bytesToRead > 0)
            {
                byte[] packet = new byte[bytesToRead];
                Array.Copy(packetBuffer, packet, bytesToRead);
                _newPackets.Enqueue(packet);
            }
            else
            {
                Debug.Log("ERROR: NULL PACKET ON SERVER!!");
            }
        }

        // Recieve handler
        public void ProcessPacket(byte[] packet)
        {
            try
            {
                if (packet.Length == 0)
                {
                    Debug.Log("ERROR: null packet on server");
                    return;
                }

                // Pop packet type
                NewPackets.PacketType type;
                packet = NewPackets.PopTypeHeader(packet, out type);

                //Debug.Log($"Server Game Manager: packet is of type {type}...");

                // Pop packet faction
                byte factionID = 0;
                packet = NewPackets.PopByteHeader(packet, out factionID);

                //Debug.Log($"Server Game Manager: packet was sent by faction {factionID}...");

                // **Packet bytes should now be castable**

                // Minimized, generalized, client request cases:
                //  - Tile interacts are used for moving and mining
                //  - Entity interacts are used for carrying, building, and combat
                switch (type)
                {
                    case NewPackets.PacketType.Deploy:
                        Debug.Log("GOT DEPLOY");
                        PlayerFactions[factionID].CustomDeploy(new NewPackets.Deploy(packet));
                        break;

                    case NewPackets.PacketType.Buy:
                        Debug.Log("TRYING TO BUY ON SERVER: " + new NewPackets.Buy(packet).type);
                        PlayerFactions[factionID].Buy(new NewPackets.Buy(packet).type);
                        break;

                    case NewPackets.PacketType.TileInteractRequest:

                        PlayerFactions[factionID].TileInteraction(new NewPackets.TileInteraction(packet));
                        break;
                    // TODO: will need to split this into different cases (based on header?)?
                    //      - Enemy units can only be combatted
                    //      - Ore faction can only be grabbed
                    //      - Self faction can be grabbed and activated
                    case NewPackets.PacketType.EntityInteractRequest:
                        PlayerFactions[factionID].EntityInteraction(new NewPackets.EntityInteraction(packet));
                        break;

                    default:
                        Debug.Log("SERVER GAME MANAGER: ERROR packet type unhandled!");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.Log($"EXCEPTION IN RECIEVE: {e} \n\n STACK TRACE: \n --------------- \n {e.StackTrace}");
            }
        }

        // TODO: break up this method
        // Nothing about this is atomic, fuck yea
        public void AddPlayer(ConnectedClient playerClient)
        {
            try
            {
                // Add a new player manager, initial faction update for client
                PlayerFactions.Add(new ServerFactionManager(
                    PlayerFactions.Count, //id
                    MapManager,           //context
                    this,                 //context
                    playerClient,         //client
                    false));              //not a neutral faction

                Debug.Log("Server Game Manager: Faction setup complete! " + PlayerFactions.Count);
            }
            catch (Exception e)
            {
                Debug.Log($"EXCEPTION IN JOIN: {e} \n\n STACK TRACE: \n --------------- \n {e.StackTrace}");
            }
        } 

        // TODO: move this somewhere nicer pls
        // dear gahd
        public void DamageAllFactionEntities(ByteVector2 from, int radius, int damage)
        {

            for (int j = 0; j < PlayerFactions.Count; j++)
            {
                ServerFactionManager f = PlayerFactions[j];
                for (int i = 0; i < f.Entities.Length; i++)
                {
                    if (f.Entities[i] != null)
                    {
                        //Debug.Log($"Distance was {ByteVector2.Distance(from, f.Entities[i].CurrentPosition)} || radius {radius} || {ByteVector2.Distance(from, f.Entities[i].CurrentPosition) <= (float)radius}");

                        if (ByteVector2.ManhattanDistance(from, f.Entities[i].GetPosition()) <= (float)radius)
                        {
                            f.Entities[i].Damage(damage);
                        }
                    }
                }
            }
        }

        private void StartGameClock()
        {
            GameWatch = new System.Diagnostics.Stopwatch();
            GameClock = new Timer(GameClockStep, null, 0, Timeout.Infinite);

            // Fix Me: handle disposal!

        }

        const int BroadcastInterval = 10;
        int step = 0;
        public long CountStep = 0;
        int tix = 0;

        
        private object _lock = new object();
        private int _locked = 2;

        private void GameClockStep(System.Object stateInfo)
        {
            // Update delta time
            GameWatch.Stop();
            TimeSpan ts = GameWatch.Elapsed;
            DeltaTime = ts.TotalSeconds;
            int deltaTimeMS = ts.Milliseconds;
            GameWatch.Reset();
            GameWatch.Start();

            try
            {
                CountStep++;
                step++;
                // Grant passive income to all factions if this is a multiplayer game
                if(PlayerFactions.Count >= 3)
                    PassiveIncome();

                // Process all newly recieved packets
                byte[] toProcess;
                while (_newPackets.TryDequeue(out toProcess))
                {
                    ProcessPacket(toProcess);
                }

                // Step all jobs and send states (10/s)
                for (int i = 0; i < PlayerFactions.Count; i++)
                {
                    if (i > 0)
                    {
                        PlayerFactions[i].StepJobs();
                    }
                    if (step % 10 == 0)
                    {
                        PlayerFactions[i].SendStates();
                    }
                }

                MapManager.TileGasManager.Refresh();

                // Debug
                if (CountStep >= 100)
                {
                    Debug.Log($"SENT [{countBytes}] BYTES | STEP [{step}]");
                    CountStep = 0;
                    countBytes = 0;
                }
            }
            catch (Exception e)
            {
                Debug.Log($"EXCEPTION IN LOOP: {e} \n\n STACK TRACE: \n --------------- \n {e.StackTrace}");
            }

            int nextStep = FixedTimeStep - deltaTimeMS;
            if (nextStep < 0) nextStep = 0;

            GameClock.Change(nextStep, Timeout.Infinite);
        }

        private void PassiveIncome()
        {
            // If an income interval is happening
            IncomeTimer += DeltaTime;
            if(IncomeTimer >= Constants.Global.INCOME_INTERVAL)
            {
                // Reset timer
                IncomeTimer -= Constants.Global.INCOME_INTERVAL;
                // Grant income
                for (int i = 0; i < PlayerFactions.Count; i++)
                {
                    PlayerFactions[i].Credits += Constants.Global.INCOME_VALUE;
                }
            }
        }

        public void Dispose()
        {
            GameClock.Dispose();
            GameServer.Dispose();
        }
    }
}
