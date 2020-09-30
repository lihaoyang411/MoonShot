using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Level : MonoBehaviour
{
    public GameStarter starter;

    public void LoadLevel(int level)
    {
        SceneManager.LoadScene(level);
    }

    public void Tutorial(int level)
    {
        starter.Tutorial = true;
        starter.Host = true;

        LoadLevel(level);
    }

}
