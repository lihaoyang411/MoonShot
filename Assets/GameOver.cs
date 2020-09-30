using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameOver : MonoBehaviour
{
    public Image BG;
    public Text TXT;
    public Color WinColor;
    public string WinText;
    public Color LoseColor;
    public string LoseText;

    public void Display(bool winning)
    {
        if(winning)
        {
            BG.color = WinColor;
            TXT.text = WinText;
        }
        else
        {
            BG.color = LoseColor;
            TXT.text = LoseText;
        }

        BG.enabled = true;
        TXT.enabled = true;
    }
}
