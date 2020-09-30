using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Numerics;

namespace TeamBlack.MoonShot.Networking
{
    public class ServerMapManager
    {
        public class MapGenerator
        {
            private Tile[,] GeneratedMap;

            public MapGenerator(byte size)
            {
                GeneratedMap = new Tile[size, size];
                System.Random gen = new System.Random();

                float caveScale = 30;
                float caveThreshold = 0.2f;

                int offsetX = gen.Next(1000000);
                int offsetY = gen.Next(1000000);
                // Cave layer
                //for (int i = 0; i < 10; i++)
                //    GeneratedMap = genTilePos(GeneratedMap, 60, 6, 2);
                for (int i = 0; i < GeneratedMap.GetLength(0); i++)
                    for (int j = 0; j < GeneratedMap.GetLength(0); j++)
                        if (Perlin.Noise(
                            offsetX + caveScale * ((float)j) / 256 + offsetX,
                            offsetY + caveScale * ((float)i) / 256 + offsetY)
                            > caveThreshold)
                        {
                            GeneratedMap[i, j] = Tile.Empty;
                        }
                        else
                        {
                            GeneratedMap[i, j] = Tile.Stone;
                        }

                offsetX = gen.Next(1000000);
                offsetY = gen.Next(1000000);
                // Combine layers
                float oreScale = 20;
                float oreThreshold = 0.4f;
                for (int i = 0; i < GeneratedMap.GetLength(0); i++)
                    for (int j = 0; j < GeneratedMap.GetLength(0); j++)
                    {
                        if (Perlin.Noise(
                            oreScale * ((float)j) / 256 + offsetX,
                            oreScale * ((float)i) / 256 + offsetY)
                            > oreThreshold)
                        {
                            GeneratedMap[i, j] = Tile.Ore;
                        }

                        //Debug.Log("NOISE: " + Perlin.Noise(((float)i)/256, (float)j) / 256);
                    }
            }

            public Tile[,] GetMap()
            {
                return GeneratedMap;
            }

            public Tile[,] genTilePos(Tile[,] oldMap, int randbound, int birthLimit, int deathLimit)
            {
                int width = oldMap.GetLength(0);
                int height = oldMap.GetLength(0);

                oldMap = oldMap ?? new Tile[width, height];

                Tile[,] newMap = new Tile[width, height];
                int neighbor;
                BoundsInt myB = new BoundsInt(-1, -1, 0, 3, 3, 1);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {//for each tile, if it's new random # is < input iniChance then it's 'alive' (1), else 'dead' (0)
                        var r = UnityEngine.Random.Range(1, 101);
                        oldMap[x, y] = r < randbound ? Tile.Stone : Tile.Empty;
                    }
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        neighbor = 0;
                        //looking at the neighbors within the map
                        foreach (var b in myB.allPositionsWithin)
                        {
                            if (b.x == 0 && b.y == 0) continue;
                            if (x + b.x >= 0 && x + b.x < width && y + b.y >= 0 && y + b.y < height)
                            {
                                neighbor += (byte)oldMap[x + b.x, y + b.y];
                            }
                            else//at border
                            {
                                neighbor++;
                            }
                        }
                        if (oldMap[x, y] == Tile.Stone)
                        {
                            if (neighbor < deathLimit) newMap[x, y] = 0;
                            else
                            {
                                newMap[x, y] = Tile.Stone;
                            }
                        }
                        if (oldMap[x, y] == 0)
                        {
                            if (neighbor > birthLimit) newMap[x, y] = Tile.Stone;
                            else
                            {
                                newMap[x, y] = 0;
                            }
                        }
                    }
                }
                return newMap;
            }
        }

        public Tile[,] Map;

        public class TriggerGrid
        {
            // Grid of listeners
            private List<Triggerable>[,] listeners;

            public TriggerGrid(int size)
            {
                listeners = new List<Triggerable>[size, size];
                for (int r = 0; r < size; r++)
                    for (int c = 0; c < size; c++)
                        listeners[r, c] = new List<Triggerable>();
            }

            public void TriggerAt(ByteVector2 position, int triggeringFaction)
            {
                for (int i = 0; i < listeners[position.X, position.Y].Count; i++)
                {
                    Triggerable l = listeners[position.X, position.Y][i];
                    if (l == null)
                    {
                        Debug.Log("ERROR TRIGGERING AT ***************************");
                        continue;
                    }

                    if(!l.InFaction(triggeringFaction)) // Can't trigger own entities
                        l.Trigger();
                }
            }

            public void ListenAt(Triggerable listener, ByteVector2 position)
            {
                Debug.Log("TRIG LISTENING");
                try
                {
                    listeners[position.X, position.Y].Add(listener);
                }
                catch (System.Exception e)
                {
                    Debug.Log("ERROR LISTENING AT ***************************" + e);
                }
            }

            public void UnlistenAt(Triggerable listener, ByteVector2 position)
            {
                try
                {
                    Debug.Log("REMOVING LISTENER : " + listeners[position.X, position.Y].Remove(listener));
                }
                catch (System.Exception e)
                {
                    Debug.Log("ERROR REMOVING LISTENER AT ***************************" + e);
                }
            }
        }

        public class OccupationGrid
        {
            // Grid of listeners
            private List<Entity>[,] entities;

            public OccupationGrid(int size)
            {
                entities = new List<Entity>[size, size];
                for (int r = 0; r < size; r++)
                    for (int c = 0; c < size; c++)
                        entities[r, c] = new List<Entity>();
            }

            public void Add(Entity entity, ByteVector2 at)
            {
                entities[at.X, at.Y].Add(entity);
            }

            public void Remove(Entity entity, ByteVector2 at)
            {
                entities[at.X, at.Y].Remove(entity);
            }

            public void Move(Entity entity, ByteVector2 from, ByteVector2 to)
            {
                // Update mapping
                entities[from.X, from.Y].Remove(entity);
                entities[to.X, to.Y].Add(entity);

                //Debug.Log($"ENTITY MOVING TO {to.X} {to.Y}");
            }

            public List<Entity> GetEntities(ByteVector2[] tiles)
            {
                List<Entity> toReturn = new List<Entity>();
                foreach (ByteVector2 b in tiles)
                {
                    foreach (Entity e in entities[b.X, b.Y])
                    {
                        toReturn.Add(e);
                    }
                }

                return toReturn;
            }
        }

        public class GasManager
        {
            private HashSet<ByteVector2> _territory;
            private List<Entity> _emitters;
            private ServerPathfinder _pathfinder;
            private ServerMapManager _mapManager;

            private double[,] _gasMap;
            private double[,] _fillMap;

            public GasManager(ServerMapManager MapManager)
            {
                _emitters = new List<Entity>();
                _pathfinder = new ServerPathfinder();
                _mapManager = MapManager;
                _territory = new HashSet<ByteVector2>();

                int length = MapManager.Map.GetLength(0);
                _gasMap = new double[length, length];
                _fillMap = new double[length, length];
            }

            public void RegisterEmitter(Entity e)
            {
                _emitters.Add(e);
            }

            public void DeregisterEmitter(Entity e)
            {
                if(_emitters.Contains(e))
                    _emitters.Remove(e);
            }

            private void FillGas()
            {
                foreach (Entity e in _emitters)
                {
                    ByteVector2 pos = e.GetPosition();
                    _gasMap[pos.X, pos.Y] =  1;
                }

                for (int r = 0; r < _gasMap.GetLength(0); r++)
                    for (int c = 0; c < _gasMap.GetLength(0); c++)
                        _fillMap[r,c] = 0;

                for (int r = 0; r < _gasMap.GetLength(0); r++)
                    for (int c = 0; c < _gasMap.GetLength(0); c++)
                    {
                        double val = _gasMap[r, c];
                        if (val > 0)
                        {
                            FillTile(val, (byte)(r + 1), (byte)(c));
                            FillTile(val, (byte)(r - 1), (byte)(c));
                            FillTile(val, (byte)(r), (byte)(c + 1));
                            FillTile(val, (byte)(r), (byte)(c - 1));
                        }

                        //_gasMap[r, c] = fifth;
                    }

                for (int r = 0; r < _gasMap.GetLength(0); r++)
                    for (int c = 0; c < _gasMap.GetLength(0); c++)
                        _gasMap[r, c] = _fillMap[r, c];

                for (int r = 0; r < _gasMap.GetLength(0); r++)
                    for (int c = 0; c < _gasMap.GetLength(0); c++)
                    {
                        _gasMap[r, c] -= Constants.Entities.GasBomb.DECAY_FACTOR;
                        //if (_gasMap[r, c] > Constants.Entities.GasBomb.GAS_MAX)
                        //    _gasMap[r, c] = Constants.Entities.GasBomb.GAS_MAX;
                        if (_gasMap[r, c] < 0)
                        {
                            _gasMap[r, c] = 0;
                        }
                    }

            }

            private void FillTile(double factor, byte x, byte y)
            {
                if (x >= _gasMap.GetLength(0) || x < 0 || y >= _gasMap.GetLength(0) || y < 0)
                    return;

                if (_mapManager.Map[x, y] != Tile.Empty && _mapManager.Map[x, y] != Tile.Gas)
                    return;

                if(factor > _fillMap[x, y])
                    _fillMap[x, y] = factor;
            }

            // Called when a block is placed/moved

            private double currentTime = 0;
            public void Refresh()
            {
                // Damage and spread on interval
                currentTime += _mapManager.GameManager.DeltaTime;
                if (currentTime >= Constants.Entities.GasBomb.ATTACK_INTERVAL)
                {
                    // Run fill calculations
                    FillGas();

                    // Refresh gas
                    for (int r = 0; r < _gasMap.GetLength(0); r++)
                        for (int c = 0; c < _gasMap.GetLength(0); c++)
                            if (_mapManager.Map[r, c] == Tile.Gas || _mapManager.Map[r, c] == Tile.Empty)
                                _mapManager.Map[r, c] = _gasMap[r, c] > 0 
                                    ? Tile.Gas : Tile.Empty;

                    // Damage
                    currentTime -= Constants.Entities.GasBomb.ATTACK_INTERVAL;
                    DamageEntities();
                }
            }

            private void DamageEntities()
            {
                List<ByteVector2> territory = new List<ByteVector2>();
                for (int r = 0; r < _mapManager.Map.GetLength(0); r++)
                    for (int c = 0; c < _mapManager.Map.GetLength(0); c++)
                        if (_mapManager.Map[r, c] == Tile.Gas)
                        {
                            territory.Add(new ByteVector2((byte)r, (byte)c));
                        }

                List<Entity> entities = _mapManager.TileOccupationGrid.GetEntities(territory.ToArray());
                foreach (Entity e in entities)
                {
                    if(e.Type <= Constants.Entities.UNIT_IDS)
                        e.Damage(Constants.Entities.GasBomb.ATTACK_DAMAGE);
                }
            }

        }

        private ServerPathfinder Pathfinder;
        public TriggerGrid TileTriggerGrid;
        public OccupationGrid TileOccupationGrid;
        public ServerGameManager GameManager;
        public GasManager TileGasManager;

        public ServerMapManager(byte mapSize, ServerGameManager context)
        {
            Debug.Log("Map Manager: Initializing map manager");
            MapGenerator gen = new MapGenerator(mapSize);
            Map = gen.GetMap();

            Pathfinder = new ServerPathfinder();
            Pathfinder.SetByteMap(Map);
            TileTriggerGrid = new TriggerGrid(mapSize);
            TileOccupationGrid = new OccupationGrid(mapSize);
            TileGasManager = new GasManager(this);
            GameManager = context;
            //DebugPrintMap();
        }

        public bool CheckLOS(ByteVector2 from, ByteVector2 to)
        {
            List<ByteVector2> tiles = GetTilesInLine(from, to);
            foreach (ByteVector2 tile in tiles)
            {
                if (Map[tile.X, tile.Y] == Tile.Empty || Map[tile.X, tile.Y] == Tile.Gas)
                    continue;

                return false;
            }

            return true;
        }

        public ByteVector2[] GetPath(ByteVector2 from, ByteVector2 to)
        {
            // Update map
            Pathfinder.SetByteMap(Map);

            if (Map[to.X, to.Y] == Tile.Empty || Map[to.X, to.Y] == Tile.Gas) // EZ case
            {
                return Pathfinder.FindBestPath(from, to);
            }
            else // The block is solid
            {
                return Pathfinder.FindBestAdjacentPath(from, to);
            }
        }

        public ByteVector2[] GetTerritory(ByteVector2 from, int depth)
        {
            Pathfinder.SetByteMap(Map);
            return Pathfinder.FindCoverage(from, depth);
        }

        public Entity[] GetEntities(ByteVector2 from, int depth)
        {
            return TileOccupationGrid.GetEntities(GetTerritory(from, depth)).ToArray();
        }

        public ByteVector2[] GetTilesInSquare(ByteVector2 center, int radius)
        {
            List<ByteVector2> tiles = new List<ByteVector2>();

            int mapSize = Map.GetLength(0);

            int upperX = center.X - radius;
            if (upperX < 0) upperX = 0;
            int upperY = center.Y + radius;
            if (upperY >= mapSize) upperY = mapSize - 1;

            int lowerX = center.X + radius;
            if (lowerX >= mapSize) lowerX = mapSize - 1;
            int lowerY = center.Y - radius;
            if (lowerY < 0) lowerY = 0;

            // Upper left
            ByteVector2 start = new ByteVector2((byte)upperX, (byte)upperY);
            // Bottom right
            ByteVector2 end = new ByteVector2((byte)lowerX, (byte)lowerY);

            for (int r = start.Y; r >= end.Y; r--)
                for (int c = start.X; c <= end.X; c++)
                    tiles.Add(new ByteVector2((byte)c, (byte)r));

            return tiles.ToArray();
        }

        public ByteVector2[] GetTilesInCircle(ByteVector2 center, int radius)
        {
            List<ByteVector2> tiles = new List<ByteVector2>();

            int mapSize = Map.GetLength(0);

            int upperX = center.X - radius;
            if (upperX < 0) upperX = 0;
            int upperY = center.Y + radius;
            if (upperY >= mapSize) upperY = mapSize - 1;

            int lowerX = center.X + radius;
            if (lowerX >= mapSize) lowerX = mapSize - 1;
            int lowerY = center.Y - radius;
            if (lowerY < 0) lowerY = 0;

            // Upper left
            ByteVector2 start = new ByteVector2((byte)upperX, (byte)upperY);
            // Bottom right
            ByteVector2 end = new ByteVector2((byte)lowerX, (byte)lowerY);

            for (int r = start.Y; r >= end.Y; r--)
                for (int c = start.X; c <= end.X; c++)
                {
                    ByteVector2 addable = new ByteVector2((byte)c, (byte)r);
                    if (ByteVector2.Distance(addable, center) <= radius)
                        tiles.Add(addable);
                }

            return tiles.ToArray();
        }

        public ByteVector2[] GetTilesInDiamond(ByteVector2 center, int radius)
        {
            List<ByteVector2> tiles = new List<ByteVector2>();

            int mapSize = Map.GetLength(0);

            int upperX = center.X - radius;
            if (upperX < 0) upperX = 0;
            int upperY = center.Y + radius;
            if (upperY >= mapSize) upperY = mapSize - 1;

            int lowerX = center.X + radius;
            if (lowerX >= mapSize) lowerX = mapSize - 1;
            int lowerY = center.Y - radius;
            if (lowerY < 0) lowerY = 0;

            // Upper left
            ByteVector2 start = new ByteVector2((byte)upperX, (byte)upperY);
            // Bottom right
            ByteVector2 end = new ByteVector2((byte)lowerX, (byte)lowerY);

            for (int r = start.Y; r >= end.Y; r--)
                for (int c = start.X; c <= end.X; c++)
                {
                    ByteVector2 addable = new ByteVector2((byte)c, (byte)r);
                    if (ByteVector2.ManhattanDistance(addable, center) <= radius)
                        tiles.Add(addable);
                }

            return tiles.ToArray();
        }

        public List<ByteVector2> GetTilesInLine(ByteVector2 from, ByteVector2 to)
        {
            // TODO: check null

            List<ByteVector2> toReturn = new List<ByteVector2>();
            System.Numerics.Vector2 dest = new System.Numerics.Vector2(to.X, to.Y);
            System.Numerics.Vector2 curStep = new System.Numerics.Vector2(from.X, from.Y);
            ByteVector2 toAdd;

            System.Numerics.Vector2 dir = new System.Numerics.Vector2(to.X, to.Y) - curStep;
            dir = System.Numerics.Vector2.Normalize(dir) / 4;

            double distance = System.Numerics.Vector2.Distance(curStep, dest);
            double curDistance = 0;

            do
            {
                toAdd = new ByteVector2((byte)curStep.X, (byte)curStep.Y);
                toReturn.Add(toAdd);
                curStep += dir;
                curDistance += 0.25f;
                
            }
            while (curDistance < distance);

            return toReturn;
        }

        public void PunchTilesInLine(ByteVector2 from, ByteVector2 to)
        {
            List<ByteVector2> toPunch = GetTilesInLine(from, to);

            foreach (ByteVector2 b in toPunch)
            {
                Map[b.X, b.Y] = Tile.Stone;
            }
        }

        public void PunchSquare(int radius, ByteVector2 location)
        {
            ByteVector2[] toPunch = GetTilesInSquare(location, radius);

            foreach (ByteVector2 b in toPunch)
            {
                Map[b.X, b.Y] = Tile.Empty;
            }
        }

        public void PunchCircle(int radius, ByteVector2 location)
        {
            ByteVector2[] toPunch = GetTilesInCircle(location, radius);

            foreach (ByteVector2 b in toPunch)
            {
                Map[b.X, b.Y] = Tile.Empty;
            }
        }

        public void PunchDiamond(int radius, ByteVector2 location)
        {
            ByteVector2[] toPunch = GetTilesInDiamond(location, radius);

            foreach (ByteVector2 b in toPunch)
            {
                Map[b.X, b.Y] = Tile.Empty;
            }
        }

        public void FillSquare(int radius, ByteVector2 location)
        {
            ByteVector2[] toFill = GetTilesInSquare(location, radius);

            foreach (ByteVector2 b in toFill)
            {
                Map[b.X, b.Y] = Tile.Stone;
            }
        }

        public void DebugPrintMap()
        {
            string output = "";
            for (int r = 0; r < Map.GetLength(0); r++)
            {
                for (int c = 0; c < Map.GetLength(0); c++)
                {
                    output += (int)Map[r, c] + " ";
                }
                output += "\n";
            }

            Debug.Log(output);
        }
    }

    
}
