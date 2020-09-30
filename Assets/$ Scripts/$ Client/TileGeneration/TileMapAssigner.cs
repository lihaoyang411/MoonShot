using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace TeamBlack.MoonShot
{
    [RequireComponent(typeof(Tilemap))]
    public class TileMapAssigner : MonoBehaviour
    {
        [SerializeField] private MapManager _myMapManager;
        
        [Range(1,8)]
        [Tooltip("Birth: Base Rock = 2, Common Rock = 4")]
        public int birthLimit = 2;

        [Range(1,8)]
        [Tooltip("Death: Base Rock = 2, Common Rock = 1")]
        public int deathLimit = 2;

        private void Awake()
        {
//            Tilemap tm = GetComponent<Tilemap>();
//            _myMapManager.OreMap = tm;
//
//            tm.ClearAllTiles();

            

            for (int i = 0; i < 10; i++)
                _myMapManager.Map = genTilePos(_myMapManager.Map, 60);


            Tile[,] oreMap = new Tile[_myMapManager.MapWidth, _myMapManager.MapHeight];
            for (int i = 0; i < 2; i++)
                oreMap = genTilePos(oreMap, 50);

            for (int i = 0; i < _myMapManager.MapWidth; i++)
            for (int j = 0; j < _myMapManager.MapHeight; j++)
            {
                if (_myMapManager[i, j] !=     Tile.Empty && oreMap[i, j] != Tile.Empty)
                    _myMapManager[i, j] = Tile.Ore;
            }

            

            for (int i = -5; i < 5; i++)
            for (int j = -5; j < 5; j++)
            {
                _myMapManager[i+_myMapManager.MapWidth/2,j+_myMapManager.MapHeight/2] = Tile.Empty;
            }
        }    
        
        public Tile[,] genTilePos(Tile[,] oldMap, int randbound)
        {
            int width = _myMapManager.MapWidth;
            int height = _myMapManager.MapHeight;

            oldMap = oldMap ?? new Tile[width, height];
            
            Tile[,] newMap = new Tile[width, height];
            int neighbor;
            BoundsInt myB = new BoundsInt(-1,-1,0,3,3,1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {//for each tile, if it's new random # is < input iniChance then it's 'alive' (1), else 'dead' (0)
                    var r = Random.Range(1, 101);
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
                        if (b.x == 0 && b.y ==0) continue;
                        if (x + b.x >= 0 && x + b.x < width && y + b.y >= 0 && y + b.y < height)
                        {
                            neighbor += (byte)oldMap[x + b.x, y + b.y];
                        }
                        else//at border
                        {
                            neighbor++;
                        }
                    }
                    if (oldMap[x,y] == Tile.Stone)
                    {
                        if (neighbor < deathLimit) newMap[x,y] = 0;
                        else
                        {
                            newMap[x,y] = Tile.Stone;
                        }
                    }
                    if (oldMap[x,y] == 0)
                    {
                        if (neighbor > birthLimit) newMap[x,y] = Tile.Stone;
                        else
                        {
                            newMap[x,y] = 0;
                        }
                    }
                }
            }
            return newMap;
        }
    }
}

    