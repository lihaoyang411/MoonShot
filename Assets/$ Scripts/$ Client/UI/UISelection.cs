using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamBlack.MoonShot;

public class UISelection : MonoBehaviour
{
    public void Start()
    {
        NeoPlayer.Instance.Selected.Listen(() =>
        {
            if (NeoPlayer.Instance.FrontSelected == null)
                return;
            else
                Debug.Log("Display: " + NeoPlayer.Instance.FrontSelected);
            });
    }
}
