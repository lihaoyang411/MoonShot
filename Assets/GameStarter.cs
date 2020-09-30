
using UnityEngine;
using TeamBlack.MoonShot.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using TeamBlack.MoonShot;

public class GameStarter : MonoBehaviour
{

    private static GameStarter _instance;
    // For hosting
    private ServerGameManager GameManager;
    // Every client has one player instance
    public GameObject GameInstancePrefab;
    public string IPAddress;
    public bool Host;
    public bool Tutorial;
    public string MainSceneName;

    private void Awake()
    {
        //GameManager = new ServerGameManager();
        if (_instance != null) Destroy(gameObject);
        _instance = this;
        
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Clock keeps running otherwise >.>
    private void OnApplicationQuit()
    {
        GameManager.Dispose();
    }

    // called second
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameManager != null)
        {
            Debug.Log("GAME MANAGER SHOULD BE NULL");
            GameManager.Dispose();
            GameManager = null;
        }
        if (scene.name.Equals(MainSceneName))
        {
            StartGame();
        }

    }

    private void StartGame()
    {
        if (Host) // "Connect to self" implies hosting
        {
            Debug.Log("HOSTING...");
            GameManager = new ServerGameManager();
        }

        StartCoroutine(DelayedJoin());
    }

    // Join the server with the provided IP
    private IEnumerator DelayedJoin()
    {
        yield return new WaitForSeconds(1);
        GameObject player = GameObject.Instantiate(GameInstancePrefab);
        GameObject.FindGameObjectWithTag("Player").GetComponent<NetClient>().Initialize(IPAddress);
        Debug.Log("JOIN COMPLETE");

        if(Tutorial)
            GameObject.FindObjectOfType<HelpMenu>().Initialize();
    }

    // for UI callbacks
    public void SetIP(string ip)
    {
        IPAddress = ip;
    }

    public void SetHost(bool host)
    {
        Host = !Host;
    }

    public void SetTutorial(bool host)
    {
        Tutorial = !Tutorial;
    }

    public void Quit()
    {
        Application.Quit();
    }
}
