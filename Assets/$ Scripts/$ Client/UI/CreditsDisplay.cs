using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace TeamBlack.MoonShot.UI
{
    public class CreditsDisplay : MonoBehaviour
    {
        [SerializeField] private Text _creditText;
        
        void Start()
        {
            // GameManager.Instance.Player.Credits.Listen(() =>  _creditText.text = $"Credits: ${GameManager.Instance.Player.Credits.Value}" );
        }
    }
}
    