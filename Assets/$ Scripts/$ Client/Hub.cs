using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TeamBlack.MoonShot
{
    public class Hub : Entity
    {
        public GameObject purchaseMenu;
        public GameObject GameOverPrefab;
        public DamagFeedback DamageFeedback;

        private AudioManager _audioManager;
        void Start()
        {
            //if (factionID == NeoPlayer.Instance.myFactionID) FindObjectOfType<DumbFog>().ClearFog(10, transform.position);
            _audioManager = GameObject.FindObjectOfType<AudioManager>();
            purchaseMenu = GameObject.FindGameObjectWithTag("PurchaseMenu");
        }

        private void OnMouseDown()
        {
            _audioManager.PlayUIConfirm();
            purchaseMenu.GetComponent<ShowHide>().ToggleMenu();
        }

        public void BuyUnit(GameObject unit)
        {
            Instantiate(unit, transform.position, Quaternion.identity);
        }

        public override void OnUpdate()
        {
            transform.position = MapManagerReference.WorldPosFromGridIndex(tilePos) + new Vector2(.5f, .5f);

        }

        public override void OnHidden()
        {
            GameObject.Instantiate(GameOverPrefab).GetComponentInChildren<Text>().text = 
                factionID == NeoPlayer.Instance.myFactionID ? "YOU LOSE" : "YOU WIN";
            base.OnHidden();
        }

        public override void OnDamage()
        {
            if (DamageFeedback != null)
            {
                DamageFeedback.PlayImpact();
            }
        }

        public override void OnHealthChange()
        {
            if (DamageFeedback != null)
            {
                DamageFeedback.SetDamageValue((float)HealthPoints / HealthCapacity);
            }
        }

        public override void OnDeath()
        {
            DamageFeedback.Die();
            base.OnDeath();
        }
    }
}
