using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class TileMapGeneration : MonoBehaviour
{
    [Space]
    [Header("Hover over each input for more info.")]
    [Header("Press BACKSPACE to delete map.")]
    [Header("Press SPACE to generate map.")]
    

    [Range(0,100)]
    [Tooltip("ini Chance: Base Rock = 40, Common Rock = 25")]
    public int iniChance;

    [Range(1,8)]
    [Tooltip("Birth: Base Rock = 2, Common Rock = 4")]
    public int birthLimit;

    [Range(1,8)]
    [Tooltip("Death: Base Rock = 2, Common Rock = 1")]
    public int deathLimit;

    [Range(1,10)]
    [Tooltip("Keep numR at 1")]
    public int numR;
    private int count = 0;

    private int[,] terrainMap;
    public Vector3Int tileMapSize;
    public Tilemap topMap;
    public Tilemap bottomMap;
    public TileBase topTile;//can be changed to TerrainTile (need to set that up first) and AnimatedTile (not as useful)
    public TileBase bottomTile;//since the bottom will be the same this is fine

    int width;//tileMapSize demensions 
    int height;

    public void doSimulation(int numR)
        {
            clearMap(false);
            width = tileMapSize.x;
            height = tileMapSize.y;

            if (terrainMap == null)
            {//if map is cleared, make new map (demensions of the map previous)
                terrainMap = new int[width, height];
                //creat map
                initPos();
            }

            for (int i = 0; i < numR; i++)//it runs the newMap numR times every click
            {//don't really understand this math, but it makes the clumps random
                terrainMap = genTilePos(terrainMap);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (terrainMap[x,y] == 1)//make 2D list here with item type
                        topMap.SetTile(new Vector3Int(-x + width / 2, -y + height / 2, 0), topTile);
                        bottomMap.SetTile(new Vector3Int(-x + width / 2, -y + height / 2, 0), bottomTile);
                }
            }
        }

    public int [,] genTilePos(int[,] oldMap)
    {
        int[,] newMap = new int[width, height];
        int neighbor;
        BoundsInt myB = new BoundsInt(-1,-1,0,3,3,1); 

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
                        neighbor += oldMap[x + b.x, y + b.y];
                    }
                    else//at border
                    {
                        neighbor++;
                    }
                }
                if (oldMap[x,y] == 1)
                {
                    if (neighbor < deathLimit) newMap[x,y] = 0;
                    else
                    {
                        newMap[x,y] = 1;
                    }
                }
                if (oldMap[x,y] == 0)
                {
                    if (neighbor > birthLimit) newMap[x,y] = 1;
                    else
                    {
                        newMap[x,y] = 0;
                    }
                }
            }
        }

        
        return newMap;
    }

    public void initPos()
    {//remake map
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {//for each tile, if it's new random # is < input iniChance then it's 'alive' (1), else 'dead' (0)
                terrainMap[x,y] = Random.Range(1,101) < iniChance ? 1 : 0;
            }
        }
    }
   

    // Update is called once per frame
    void Update()
    {
        //left mouse runs simulation again
        if (Input.GetKeyDown("space"))
        {
            doSimulation(numR);
        }

        //right mouse clears map
        if (Input.GetKeyDown("backspace"))
        {
            clearMap(true);
        }

        //middle mouse to save map as prefab
        // if (Input.GetMouseButtonDown(2))
        // {
        //     SaveAssetMap();
        //     count++;

        // }
    }

    // public void SaveAssetMap()
    // {
    //     string saveName = "tmapXY_" + count;
    //     var mf = GameObject.Find("Grid");

    //     if(mf)
    //     {
    //         var savePath = "Assets/" + saveName + ".prefab";
    //         if (PrefabUtility.CreatePrefab(savePath, mf))
    //         {
    //             EditorUtility.DisplayDialog("Tilemap saved", "Your Tilemap was saved under" + savePath, "Continue");
    //         }
    //         else
    //         {
    //             EditorUtility.DisplayDialog("Tilemap NOT saved", "An ERROR occured while trying to save Tilemap under" + savePath, "Continue");
    //         }
    //     }
    // }

    public void clearMap(bool complete)
    {//clearing the map

        topMap.ClearAllTiles();
        bottomMap.ClearAllTiles();

        if (complete)
        {
            terrainMap = null;
        }
    }
}
