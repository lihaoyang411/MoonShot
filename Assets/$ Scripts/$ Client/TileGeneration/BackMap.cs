using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TeamBlack.MoonShot
{
    [RequireComponent(typeof(Tilemap))]
    public class BackMap : MonoBehaviour
    {
        [SerializeField] MapManager _myMapManager;
        [SerializeField] TileBase _backTile;

        private void Awake()
        {
            Tilemap tm = GetComponent<Tilemap>();

            int width = _myMapManager.MapWidth;
            int height = _myMapManager.MapHeight;

            for (int x = -width / 2; x < width / 2; x++)
                for (int y = -height / 2; y < height / 2; y++)
                    tm.SetTile(new Vector3Int(x, y, 0), _backTile);
        }
    }
}

