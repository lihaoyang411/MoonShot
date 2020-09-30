using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeamBlack.MoonShot
{

    class ProximityMine : Entity
    {
        public GameObject ExplosionPrefab;

        public void Start()
        {
            // FIX ME: don't send hidden entities lol

            if(factionID != 0)
                if(Type == Constants.Entities.SandBags.ID)
                    GameObject.FindObjectOfType<AudioManager>().PlaySandbagDeploy();
                else
                    GameObject.FindObjectOfType<AudioManager>().PlayDeploy();
        }

        public override void OnUpdate()
        {
            transform.position = MapManagerReference.WorldPosFromGridIndex(tilePos) + new Vector2(.5f, .5f);
        }

        public override void OnHidden()
        {
            base.OnHidden();
        }

        public override void OnDeath()
        {
            if(ExplosionPrefab != null)
                GameObject.Instantiate(ExplosionPrefab).transform.position = transform.position;
            base.OnDeath();
        }
    }
}
