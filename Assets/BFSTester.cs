using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamBlack.MoonShot;
using TeamBlack.MoonShot.Networking;

public class BFSTester : MonoBehaviour
{
    public MapManager mapManager;
    public ByteVector2 from;
    public int depth;

    public GameObject coverageMarker;

    // Start is called before the first frame update
    void Start()
    {

       Tile[,] map = mapManager.Map;

       byte[,] byteMap = new byte[map.GetLength(0), map.GetLength(0)];

       for (int y = 0; y < map.GetLength(0); y++)
       {
           for (int x = 0; x < map.GetLength(0); x++)
           {
               byteMap[y, x] = (byte)map[x, y];
           }
       }

       BFSGrid bfs = new BFSGrid(byteMap, from, depth);
       ShowCoverage(bfs.GetCoverage());
    }

    private void ShowCoverage(ByteVector2[] coverage)
    {
        foreach (ByteVector2 b in coverage)
        {
            GameObject.Instantiate(coverageMarker).transform.position =
                mapManager.WorldPosFromGridIndex(new Vector2Int(b.Y, b.X)) +
                new Vector2(0.5f, 0.5f);
        }
    }

}
