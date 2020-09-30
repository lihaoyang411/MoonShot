using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeamBlack.MoonShot
{
    public class ItemEntity : Entity
    {
        public override void OnUpdate()
        {
            transform.position = MapManagerReference.WorldPosFromGridIndex(tilePos) + new Vector2(.5f, .5f);
        }
    }
}

