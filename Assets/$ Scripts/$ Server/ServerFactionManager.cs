using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace TeamBlack.MoonShot.Networking
{
    // TODO: 
    // - [CHECK] Clustering
    // - Placement "shot" heuristic
    // - [FIX BUG ? -- Change Prefab] Bigger map
    // - Tile blink
    // - byte to int
    public class ServerFactionManager
    {
        // Context variables
        public ConnectedClient FactionClient;
        public int FactionID; // What player is this?
        public ServerGameManager GameManager;
        public ServerMapManager MapManager;

        // Faction variables
        public Entity[] Entities = new Entity[Constants.Global.MAX_ENTITIES];
        public int Credits;

        public Tile[,] FactionMap;
        public HashSet<ByteVector2> Territory;
        private HashSet<ByteVector2> _toReveal;
        private ServerPathfinder _pathfinder;
        private bool _neutral;

        //TODO: lower max index
        public int _maxIndex = 0;
        public int GetFreeEntityIndex()
        {
            for (int i = 0; i < Entities.Length; i++)
            {
                if (Entities[i] == null)
                {
                    Debug.Log("FACTION MANAGER: Found free entity index at " + i);
                    if (i > _maxIndex)
                        _maxIndex = i;
                    return i;
                }
            }

            Debug.Log("ERROR: No free indices in entity list!");
            return -1;
        }

        public bool AddEntity(byte type, ByteVector2 position)
        {
            int freeIndex = GetFreeEntityIndex();
            if (freeIndex < 0)
            {
                Debug.Log("ERROR: No free index!");
                return false;
            }

            Context context = new Context(freeIndex, FactionID, GameManager);

            Entity toAdd = null;
            switch (type)
            {
                // Hub
                case Constants.Entities.Hub.ID:
                    toAdd = new Hub(position, context);
                    break;

                // Dummy items
                case Constants.Entities.Ore.ID:
                    toAdd = new Ore(position, context);
                    break;

                // Units
                case Constants.Entities.Miner.ID:
                    toAdd = new Miner(position, context);
                    break;
                case Constants.Entities.Digger.ID:
                    toAdd = new Digger(position, context);
                    break;
                case Constants.Entities.Soldier.ID:
                    toAdd = new Soldier(position, context);
                    break;
                case Constants.Entities.Hauler.ID:
                    toAdd = new Hauler(position, context);
                    break;

                // Deployables
                case Constants.Entities.ProximityMine.ID:
                    toAdd = new ProximityMine(position, context);
                    break;
                case Constants.Entities.TimeBomb.ID:
                    toAdd = new TimeBomb(position, context); 
                    break;
                case Constants.Entities.QuickSand.ID:
                    toAdd = new QuickSand(position, context);
                    break;
                case Constants.Entities.GasBomb.ID:
                    toAdd = new GasBomb(position, context);
                    break;
                case Constants.Entities.HealStation.ID:
                    toAdd = new HealStation(position, context);
                    break;
                case Constants.Entities.SonicBomb.ID:
                    toAdd = new SonicBomb(position, context);
                    break;
                case Constants.Entities.AntimatterBomb.ID:
                    toAdd = new AntimatterBomb(position, context);
                    break;
                case Constants.Entities.SandBags.ID:
                    toAdd = new SandBags(position, context);
                    break;

            }

            if (toAdd == null)
            {
                Debug.Log("ERROR: Failed to add entity!");
                return false;
            }

            Entities[freeIndex] = toAdd;
            return true;
        }

        public ServerFactionManager(int id, ServerMapManager mapManagerReference, ServerGameManager gameManagerReference, ConnectedClient fClient = null, bool neutral = true)
        {
            Debug.Log("Faction Manager: Initializing player faction...");
            // TODO: genericize context?
            FactionID = id;
            GameManager = gameManagerReference;
            MapManager = mapManagerReference;
            FactionClient = fClient;
            Credits = Constants.Global.START_CREDITS;

            FactionMap = new Tile[MapManager.Map.GetLength(0), MapManager.Map.GetLength(0)];
            for (int i = 0; i < FactionMap.GetLength(0); i++)
                for (int j = 0; j < FactionMap.GetLength(0); j++)
                    FactionMap[i, j] = Tile.Fog;

            _pathfinder = new ServerPathfinder();
            Territory = new HashSet<ByteVector2>();
            _toReveal = new HashSet<ByteVector2>();

            if (!neutral)
            {
                SendInit();
                CrashDown();
                SendMap();
            }

            _neutral = neutral;
        }

        public void BroadcastState()
        {
            NewPackets.FactionUpdate factionUpdate = new NewPackets.FactionUpdate();
            factionUpdate.credits = Credits;
            factionUpdate.connected = true;

            GameManager.Broadcast((byte)FactionID, NewPackets.PacketType.UpdateFaction, factionUpdate.ByteArray());
        }

        public void SendMap()
        {
            if (FactionClient == null)
                return;

            NewPackets.MapUpdate mapUpdate;
            mapUpdate.mapSize = (byte)FactionMap.GetLength(0);
            mapUpdate.map = FactionMap;
            GameManager.Send(NewPackets.PacketType.MapUpdate, mapUpdate.ByteArray(), FactionClient);
        }

        public void RevealTiles(ByteVector2[] tiles)
        {
            foreach (ByteVector2 tile in tiles)
                _toReveal.Add(tile);
        }

        public void SendVisible()
        {
            //Debug.Log("SENDING VISIBLE...");

            // Send updated tiles that are in range
            ByteVector2[] visibleTiles = GetVisibleTiles();

            // Init map chunk packet
            NewPackets.ChunkUpdate chunkUpdate = new NewPackets.ChunkUpdate();
            chunkUpdate.ChunkMembers = new List<NewPackets.ChunkUpdate.ChunkMember>();

            //Debug.Log("SENDING TILES...");

            for (int i = 0; i < visibleTiles.Length; i++)
            {
                if (FactionMap[visibleTiles[i].X, visibleTiles[i].Y] != MapManager.Map[visibleTiles[i].X, visibleTiles[i].Y])
                {
                    FactionMap[visibleTiles[i].X, visibleTiles[i].Y] = MapManager.Map[visibleTiles[i].X, visibleTiles[i].Y];
                    chunkUpdate.ChunkMembers.Add(new NewPackets.ChunkUpdate.ChunkMember(
                        visibleTiles[i].X, 
                        visibleTiles[i].Y,
                        FactionMap[visibleTiles[i].X, visibleTiles[i].Y]));
                }
            }

            //Debug.Log("SENDING ENTITIES...");

            // Send new/updated tiles
            if(!_neutral)
                GameManager.Send(NewPackets.PacketType.ChunkUpdate, chunkUpdate.ByteArray(), FactionClient);

            int sent = 0;

            for (int i = 0; i <= _maxIndex; i++)
            {
                if (Entities[i] != null)
                {
                    Entities[i].SendState();
                    sent++;
                }
            }

            //Debug.Log("Sent Entities: " + sent);

            //// Send states of visible entities from other factions
            //List<Entity> others = MapManager.TileOccupationGrid.GetEntities(visibleTiles);

            //for (int i = 0; i < others.Count; i++)
            //{
            //    if (others[i] == null)
            //    {
            //        Debug.Log("ERROR: NULL ENTITY WAS RETURNED IN VISIBLE");
            //        continue;
            //    }

            //    // Send other (non triggerable) entities in range
            //    //if(!others[i].IsInFaction(FactionID) 
            //    // && others[i].GetType() != typeof(TriggerableEntity))

            //    others[i].SendState(default, FactionClient);
            //}
        }

        public void SendTileUpdate(byte x, byte y)
        {
            NewPackets.TileUpdate tileUpdate;
            tileUpdate.tileX = x;
            tileUpdate.tileY = y;
            tileUpdate.type = (byte)FactionMap[x, y];
            GameManager.Send(NewPackets.PacketType.TileUpdate, tileUpdate.ByteArray(), FactionClient);
        }

        private ByteVector2[] GetVisibleTiles()
        {
            Territory = new HashSet<ByteVector2>();

            for (int i = 0; i < Entities.Length; i++)
            {
            // For every non null entity
                if (Entities[i] != null)
                {

                    // BFS from entity
                    ByteVector2[] tiles = Entities[i].GetTerritory();

                    // Add visible tiles to set
                    foreach(ByteVector2 tile in tiles)
                        Territory.Add(tile);
                }
            }

            // DEBUG: SEND WHOLE MAP!!
            //for (int i = 0; i < MapManager.Map.GetLength(0); i++)
            //    for (int j = 0; j < MapManager.Map.GetLength(0); j++)
            //        Territory.Add(new ByteVector2((byte)i, (byte)j));

            if (_toReveal.Count > 0)
            {
                foreach (ByteVector2 b in _toReveal)
                    Territory.Add(b);
                _toReveal = new HashSet<ByteVector2>();
            }

            ByteVector2[] toReturn = new ByteVector2[Territory.Count];
            Territory.CopyTo(toReturn);
            return toReturn;
        }

        public void SendInit()
        {
            NewPackets.FactionUpdate factionUpdate = new NewPackets.FactionUpdate();
            factionUpdate.credits = Credits;
            factionUpdate.connected = true;

            Debug.Log("SENDING INIT FOR FACTION "+FactionID);

            GameManager.Send((byte)FactionID, NewPackets.PacketType.InitSelf, factionUpdate.ByteArray(), FactionClient);
        }

        // MUST be called ONLY ONCE on construction
        private void CrashDown()
        {
            // Generate initial positions
            System.Random gen = new System.Random();

            ByteVector2 HubLocation = new ByteVector2(
                (byte)gen.Next(10, MapManager.Map.GetLength(0) - 10),
                (byte)gen.Next(10, MapManager.Map.GetLength(0) - 10)
            );

            ByteVector2 InitialUnitLocation_1 = new ByteVector2((byte)(HubLocation.X + 3), HubLocation.Y);
            ByteVector2 InitialUnitLocation_2 = new ByteVector2((byte)(HubLocation.X - 3), HubLocation.Y);
            ByteVector2 InitialUnitLocation_3 = new ByteVector2(HubLocation.X, (byte)(HubLocation.Y - 3));

            MapManager.PunchCircle (7, new ByteVector2((byte)(HubLocation.X), (byte)(HubLocation.Y - 2)));

            AddEntity(Constants.Entities.Hub.ID, HubLocation);
            AddEntity(Constants.Entities.Miner.ID, InitialUnitLocation_2);
            AddEntity(Constants.Entities.Digger.ID, InitialUnitLocation_1);
            AddEntity(Constants.Entities.Hauler.ID, InitialUnitLocation_3);
        }

        public void StepJobs()
        {
            for (int i = 0; i <= _maxIndex; i++)
            {
                IWorker worker = Entities[i] as IWorker;
                if (worker == null)
                    continue;

                worker.Step();
            }

            TryGameOver();
        }

        public void TryGameOver()
        {
            if(Entities[0] == null)
            {
                NewPackets.GameOver gameOver;
                if(FactionID == 1)
                    gameOver.WinningFaction = 2;
                else
                    gameOver.WinningFaction = 1;

                GameManager.Broadcast(NewPackets.PacketType.GameOver, gameOver.ByteArray());
            }
        }

        public void SendStates()
        {
            if (!_neutral)
                BroadcastState();
            try
            {
                SendVisible();
            }
            catch(Exception e)
            {

                Debug.Log("ERROR SENDING VIS " + e);
            }
        }

        //////////// API ////////////
        // Essentially packet handlers
        // TODO: This can be condensed a lot

        public bool Buy(byte type)
        {
            ByteVector2 hubLocation = Entities[0].GetPosition();
            ByteVector2 spawnLocation = new ByteVector2(hubLocation.X, (byte)(hubLocation.Y - 2));

            switch (type)
            {
                case Constants.Entities.Miner.ID:
                    if (SpendCredits(10))
                    {
                        AddEntity(Constants.Entities.Miner.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.Digger.ID:
                    if (SpendCredits(10))
                    {
                        AddEntity(Constants.Entities.Digger.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.Soldier.ID:
                    if (SpendCredits(30))
                    {
                        AddEntity(Constants.Entities.Soldier.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.Hauler.ID:
                    if (SpendCredits(20))
                    {
                        AddEntity(Constants.Entities.Hauler.ID, spawnLocation);
                        return true;
                    }
                    break;

                //...

                case Constants.Entities.ProximityMine.ID:
                    if (SpendCredits(5))
                    {
                        GameManager.PlayerFactions[0].AddEntity(Constants.Entities.ProximityMine.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.TimeBomb.ID:
                    if (SpendCredits(5))
                    {
                        GameManager.PlayerFactions[0].AddEntity(Constants.Entities.TimeBomb.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.QuickSand.ID:
                    if (SpendCredits(5))
                    {
                        GameManager.PlayerFactions[0].AddEntity(Constants.Entities.QuickSand.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.GasBomb.ID:
                    if (SpendCredits(100))
                    {
                        GameManager.PlayerFactions[0].AddEntity(Constants.Entities.GasBomb.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.HealStation.ID:
                    if (SpendCredits(15))
                    {
                        GameManager.PlayerFactions[0].AddEntity(Constants.Entities.HealStation.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.SonicBomb.ID:
                    if (SpendCredits(2))
                    {
                        GameManager.PlayerFactions[0].AddEntity(Constants.Entities.SonicBomb.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.AntimatterBomb.ID:
                    if (SpendCredits(200))
                    {
                        GameManager.PlayerFactions[0].AddEntity(Constants.Entities.AntimatterBomb.ID, spawnLocation);
                        return true;
                    }
                    break;
                case Constants.Entities.SandBags.ID:
                    if (SpendCredits(3))
                    {
                        GameManager.PlayerFactions[0].AddEntity(Constants.Entities.SandBags.ID, spawnLocation);
                        return true;
                    }
                    break;
            }

            return false;
        }

        private bool SpendCredits(int amount)
        {
            if (amount > Credits)
                return false;

            Credits -= amount;
            return true;
        }

        public bool TileInteraction(NewPackets.TileInteraction parameters)
        {
            // Verify index range
            if (parameters.entityIndex >= Entities.Length || parameters.entityIndex < 0)
                return false;

            // This entity is the actor
            // TODO: reduce casting
            Entity subject = Entities[parameters.entityIndex] as Entity;

            if (subject == null)
            {
                Debug.Log("ERROR: Entity does not exist!");
                return false;
            }


            ByteVector2 tile = new ByteVector2(parameters.tileX, parameters.tileY);

            // If the entity is `close` to the tile and the tile is solid
            if (MapManager.Map[parameters.tileX, parameters.tileY] != Tile.Empty 
            &&  ByteVector2.ManhattanDistance(subject.GetPosition(), tile) <= 1)
            {
                // Try to dig with that entity
                IDigger digger = subject as IDigger;
                if(digger != null)
                    digger.Dig(tile);
            }

            // If the entity isn't on that tile
            else if (ByteVector2.ManhattanDistance(subject.GetPosition(), tile) > 0)
            {
                IMover mover = subject as IMover;
                if (mover != null)
                    mover.Move(tile);
            }

            return true;
        }

        // TODO: clean up the casting in this method
        public bool EntityInteraction(NewPackets.EntityInteraction parameters) 
        {
            //Debug.Log($"Entity Interaction | E1: {FactionID} {parameters.myEntityIndex} | E2: {parameters.otherFactionIndex} {parameters.otherEntityIndex}!");

            ICarrier carrier = Entities[parameters.myEntityIndex] as ICarrier;
            if (carrier == null)
                return false;

            if (parameters.otherFactionIndex == 0)                                                      // Entity is in the neutral faction (pickup)
            {
                carrier.Carry(GameManager.PlayerFactions[0].Entities[parameters.otherEntityIndex]);
            }
            else if (parameters.otherFactionIndex == FactionID)                                         // Entity is in own faction
            {
                if (parameters.myEntityIndex == parameters.otherEntityIndex)                            // "self click" defines a pop-deploy request
                    carrier.TryDeploy();
                else                                                                                    // Otherwise pass
                    carrier.Pass(Entities[parameters.otherEntityIndex]);
            }

            return true;
        }

        public void CustomDeploy(NewPackets.Deploy parameters)
        {
            if (Entities[parameters.EntityID] == null)
            {
                Debug.Log("ERROR: invalid deploy request; null entity");
                return;
            }

            ICarrier carrier = Entities[parameters.EntityID] as ICarrier;

            if (carrier != null)
            {
                Debug.Log("Valid custom deploy detected from commandable entity : " + Entities[parameters.EntityID].GetType());

                carrier.TryDeploy(parameters.InventoryID);
            }
        }
    }
}
