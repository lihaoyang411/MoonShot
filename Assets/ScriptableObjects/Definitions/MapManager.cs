using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Tilemaps;
using TeamBlack.MoonShot.Networking;

namespace TeamBlack.MoonShot
{
    public enum Tile : byte { Empty, Stone, Ore, Fog, Gas }
    
    
    
    public class MapManager : ScriptableObject
    {
        private OreMap _oreMap;
        public OreMap OreMap {
            get
            {
                return _oreMap ? _oreMap : (_oreMap = FindObjectOfType<OreMap>());
            }
            set { _oreMap = value; }
    } 
        // public Client Client;        
        public List<OreTile> WallTiles;
        

        public Tile this[int i, int j]
        {
            set
            {
                if (i > _map.GetLength(0) || j > _map.GetLength(0))
                    return;
                if (_map == null) _map = new Tile[255, 255];
                _map[i,j] = value;
                var tile = WallTiles[(byte)value];
                OreMap.SetTile(IndexToCell(i,j), value);
            }
            
            get
            {
                if (i < 0 || i >= Map.GetLength(0)
                          || j < 0 || j >= Map.GetLength(1))
                {
                    Debug.LogWarning("out of bounds of map");
                    return Tile.Fog;
                }
                if (Map[i, j] == Tile.Gas)
                {
                    return Tile.Empty;
                }
                return Map[i, j];
            }
        }
        
        public Tile this[Vector2Int v] 
        {
            set 
            {
                this[v.x, v.y] = value;
            }
            get 
            {
                return this[v.x, v.y];
            }
        }

        public Tile this[Vector2 v]
        {
            set { this[GridIndexFromWorldPos(v)] = value; }
            get { return this[GridIndexFromWorldPos(v)]; }
        }

        
        private Tile[,] _map;
        public Tile[,] Map
        {
            set
            {
                if (_map == null) _map = new Tile[255, 255];
                _map = value;
                PopulateTiles();
            }
            get { return _map; }
        }

        [SerializeField] private byte _mapWidth;
        [SerializeField] private byte _mapHeight;
        public int MapWidth => _mapWidth;
        public int MapHeight => _mapHeight;

        public Vector2Int CellToIndex(Vector3Int cellIndex)
        {
            var width = Map.GetLength(0);
            var height = Map.GetLength(1);
            
            return new Vector2Int(cellIndex.x + width/2, cellIndex.y + height/2);
        }
        public Vector3Int IndexToCell(Vector2Int cellIndex)
        {
            var width = Map.GetLength(0);
            var height = Map.GetLength(1);

            return new Vector3Int(cellIndex.x - width / 2, cellIndex.y - height / 2, 0);
        } 
        public Vector3Int IndexToCell(int x, int y)
        {
            var width = Map.GetLength(0);
            var height = Map.GetLength(1);

            return new Vector3Int(x - width / 2, y - height / 2, 0);
        }
        

        /// <summary>
        /// Updates the tilemap based on map
        /// </summary>
        public void PopulateTiles()
        {    
            // create a mapping from id to tile
            // int maxTileID = 0;
            // foreach (var wallTile in _wallTiles)
            //     maxTileID = (maxTileID < (int) wallTile.TileID) ? 
            //         (int)wallTile.TileID : maxTileID;
       
            // OreTile[] wallTiles = new OreTile[maxTileID+1];
            
            // foreach (var wallTile in _wallTiles)
            //     _wallTiles[(int) wallTile.TileID] = wallTile;
            
            // Generate the tiles appropriately 
            var width = Map.GetLength(0);
            var height = Map.GetLength(1);
            
            for (var x = 0; x <  width; x++)
            for (var y = 0; y < height; y++)
            {
                Tile currTile = Map[x, y];
                OreMap.SetTile(IndexToCell(x, y), currTile);
            }
        }

        public Vector2Int GridIndexFromWorldPos(Vector3 pos)
        {
            Vector3Int cellPos = OreMap.WorldToCell(pos);
            return CellToIndex(cellPos);
        }

        public Vector2 WorldPosFromGridIndex(Vector2Int position) {

            Vector2 pos = OreMap.CellToWorld(IndexToCell(position));
            return pos;
        }

        #region Callbacks

        public void RecieveBlockUpdate(byte[] packet) 
        {
            NewPackets.TileUpdate tileUpdate = new NewPackets.TileUpdate(packet);

            this[tileUpdate.tileX, tileUpdate.tileY] = (Tile)tileUpdate.type;
        }

        public void RecieveMapUpdate(byte[] packet)
        {
            Debug.Log("SETTING MAP");
            NewPackets.MapUpdate mapUpdate = new NewPackets.MapUpdate(packet);
            _mapWidth = (byte) mapUpdate.map.GetLength(0);
            _mapWidth = (byte) mapUpdate.map.GetLength(1);
            Map = mapUpdate.map;
        }

        public void RecieveChunkUpdate(byte[] packet)
        {
            NewPackets.ChunkUpdate chunkUpdate = new NewPackets.ChunkUpdate(packet);
            foreach (NewPackets.ChunkUpdate.ChunkMember c in chunkUpdate.ChunkMembers)
            {
                this[c.x, c.y] = c.value;
            }
        }

        #endregion

    }     
}