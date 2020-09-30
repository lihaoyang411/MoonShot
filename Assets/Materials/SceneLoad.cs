using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour
{
    //[SerializeField]
    //private Image progressBar;
    private bool completed;
    [SerializeField]
    private int scene;
    void Start()
    {
        completed = false;
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync() {
        yield return null;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone) {
            //progressBar.fillAmount = asyncLoad.progress;
            if (asyncLoad.progress >= 0.9f && completed) {
                Debug.Log("Loading Screen Complete.");
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    IEnumerator Wait(int s, AsyncOperation a) {
        yield return new WaitForSeconds(s);
        //a.allowSceneActivation = true;
    }

    public void ActivateScene() {
        completed = true;
    }
}
