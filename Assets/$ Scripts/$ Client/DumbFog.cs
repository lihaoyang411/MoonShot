using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using TeamBlack.MoonShot;
using UnityEngine;
using UnityEngine.Tilemaps;
using Tile = TeamBlack.MoonShot.Tile;


public class DumbFog : MonoBehaviour
{
    public static DumbFog Instance => FindObjectOfType<DumbFog>();
    public Tilemap Fogmap;
    public TileBase FogTile;

    void Start()
    {
        for (int i =  -256; i < 256; i++)
            for (int j = -256; j < 256; j++)
            {
                Fogmap.SetTile(NeoPlayer.Instance.MapManager.IndexToCell(i, j), FogTile);
            }
    }

    private bool inBounds(Vector2Int v)
    {
        return v.x >= 0 && v.y >= 0 && v.x < NeoPlayer.Instance.MapManager.MapWidth &&
               v.y < NeoPlayer.Instance.MapManager.MapHeight;
    }

    public void ClearFog(int radius, Vector2 pos)
    {
        var realpos = NeoPlayer.Instance.MapManager.GridIndexFromWorldPos(pos);
        var realrealpos = new Vector3Int(realpos.x, realpos.y, 0);
        for (int i = -radius; i <= radius; i++)
            for (int j = -radius; j <= radius; j++)
            {
                if (new Vector2Int(i, j).magnitude + float.Epsilon <= radius)
                    Fogmap.SetTile(NeoPlayer.Instance.MapManager.IndexToCell(i, j) +
                    realrealpos, null);
            }
    }
}