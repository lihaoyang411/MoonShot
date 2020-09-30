using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TeamBlack.MoonShot
{
    [CreateAssetMenu(fileName = "New Ore Rule Tile", menuName = "Tiles/Ore Tile")]
    public class OreTile : RuleTile
    {
        public TeamBlack.MoonShot.Tile TileID;
        
    }
}