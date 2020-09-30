using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeamBlack.MoonShot
{

    public class MoneyParticleEffect : MonoBehaviour
    {
        public GameObject gainMoney;
        public GameObject loseMoney;
        public GameObject currencyText;
        private int previousMoney;
        private int currentMoney;



    //     void Start(){
    //         // GameManager.Instance.Player.Credits.Listen(() =>  _creditText.text = $"Credits: ${GameManager.Instance.Player.Credits.Value}" );
    //         previousMoney = GameManager.Instance.Player.Credits.Value;
    //     }

    //     void Update(){
    //         currentMoney = GameManager.Instance.PlayCer.Credits.Value;
    //         Debug.Log(currentMoney);

    //         if (previousMoney != currentMoney){
    //             Debug.Log("money changed");
    //             if (previousMoney < currentMoney){
    //                 //play loseMoney
    //                 var particleFX = Instantiate(loseMoney, currencyText.transform.position, Quaternion.identity) as GameObject;
    //                 particleFX.transform.SetParent(currencyText.transform);
    //                 var ps = particleFX.GetComponent<ParticleSystem>();
    //                 Destroy (particleFX, ps.main.duration + ps.main.startLifetime.constantMax);

    //             }

    //             else if( previousMoney > currentMoney){
    //                 //play gainMoney
    //                 var particleFX2 = Instantiate(gainMoney, currencyText.transform.position, Quaternion.identity) as GameObject;
    //                 particleFX2.transform.SetParent(currencyText.transform);
    //                 var ps = particleFX2.GetComponent<ParticleSystem>();
    //                 Destroy (particleFX2, ps.main.duration + ps.main.startLifetime.constantMax);
    //             }

    //             previousMoney = currentMoney;
    //         }

    //     }
    }
}
