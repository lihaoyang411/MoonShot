using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamBlack.MoonShot.Networking;
using System.Threading;
using TeamBlack.MoonShot;
using UnityEngine.UI;

public class GameServerComponent : MonoBehaviour
{
    private ServerGameManager GameManager;

    private void Awake()
    {
        GameManager = new ServerGameManager();
    }

    // Clock keeps running otherwise >.>
    private void OnApplicationQuit()
    {
        GameManager.Dispose();
    }
}
