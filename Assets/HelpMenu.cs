using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TeamBlack.MoonShot;


public class HelpMenu : MonoBehaviour
{
    public Text Display;
    public GameObject TutorialWindow;
    public GameObject ButtonPrompt;

    private AudioManager _audioManager;
    private Coroutine _blurbRoutine;

    void Awake()
    {
        TutorialWindow.SetActive(false);
        _audioManager = GameObject.FindObjectOfType<AudioManager>();
    }

    public void Initialize() {
        StartCoroutine(Tutorial());
    }

    public IEnumerator Tutorial()
    {
        TutorialWindow.SetActive(true);
        DoDisplayBlurb("So you're the new field manager... Is this really the best HR can do?");
        yield return DetectButton();
        DoDisplayBlurb("<i>*Sigh*</i> The name is Rocktop, but you can call me sir.");
        yield return DetectButton();
        DoDisplayBlurb("Let's get this over with.");
        yield return DetectButton();
        DoDisplayBlurb("You can move the camera with <color=lime>[WASD]</color> and zoom with <color=lime>[SCROLL]</color>. You can also pan with <color=lime>[MIDDLE_CLICK]</color>.");
        yield return DetectButton();
        DoDisplayBlurb("Press <color=lime>[H]</color> to focus on your hub.");
        yield return DetectButton(KeyCode.H);

        DoDisplayBlurb("These units are here to process ore for the company. Your job is to manage them.");
        yield return DetectButton();
        DoDisplayBlurb("Select a digger unit with <color=lime>[LEFT_CLICK]</color>. The digger looks like a unicorn.");
        yield return DetectSelect();
        DoDisplayBlurb("Now <color=lime>[RIGHT_CLICK]</color> and <color=lime>[DRAG]</color> to mark an area for processing.");
        yield return DetectMine();
        DoDisplayBlurb("Those digger units can only destroy rock. Use them to traverse the asteroid and locate ore veins.");
        yield return DetectButton();
        DoDisplayBlurb("Miners drill ore to make us money. Money is good.");
        yield return DetectButton();
        DoDisplayBlurb("Command your miner to process some ore. If you cannot see any ore veins try exploring with your digger.");
        yield return DetectOre();
        DoDisplayBlurb("Miners work much slower than diggers, but when they process an ore vein you will get a moon rock.");
        yield return DetectButton();
        DoDisplayBlurb("Now select your hauler and command it to transport some moon rocks. Remember, <color=lime>[RIGHT_CLICK]</color> and <color=lime>[DRAG]</color> to mark an area for processing.");
        yield return DetectCredits(1);
        DoDisplayBlurb("All units can carry items, but the hauler excels with its speed and inventory size.");
        yield return DetectButton();
        DoDisplayBlurb("Speaking of items... The company budgets you a certain number of credits to spend in the shop. <color=lime>[LEFT_CLICK]</color> the hub to open the shop.");
        yield return DetectShop();

        DoDisplayBlurb("Here you can purchase more miners and haulers to increase your cash-flow, or soldiers to defend the company's stake from undesirables.");
        yield return DetectButton();
        DoDisplayBlurb("I for one fancy the economic value of the landmine. Purchase me a landmine, serf.");
        yield return DetectButton();
        DoDisplayBlurb("Command a selected unit to retrieve a landmine with <color=lime>[RIGHT_CLICK]</color>. Deploy it by clicking the icon in the unit's inventory panel.");
        yield return DetectButton();
        DoDisplayBlurb("Land mines are undetectable when deployed. They trigger when an enemy steps on them.");
        yield return DetectButton();
        DoDisplayBlurb("Of course there are many other fun and lethal gadgets available in the shop.");
        yield return DetectButton();
        DoDisplayBlurb("Feel free to experiment with them here, but don't take too much company time. Your real opponent is waiting.");
        yield return DetectButton();
        DoDisplayBlurb("And if you want more credits, you will just have to gather more moon rocks.");
        yield return DetectButton();
        TutorialWindow.SetActive(false);
    }

    public void DoDisplayBlurb(string text)
    {
        TutorialWindow.SetActive(true);
        if(_blurbRoutine != null)
            StopCoroutine(_blurbRoutine);
        _blurbRoutine = StartCoroutine(DisplayBlurb(text));
    }

    public IEnumerator DisplayBlurb(string text)
    {
        string[] words = text.Split(' ');
        Display.text = "";

        for(int i = 0; i < words.Length; i++)
        {
            Display.text += words[i] + " ";
            _audioManager.PlayRatchet();
            yield return new WaitForSeconds(0.03f);
        }
    }

    public IEnumerator DetectButton(KeyCode button = KeyCode.Return)
    {
        if(button == KeyCode.Return)
            ButtonPrompt.SetActive(true);
        while (true)
        {
            yield return null;
            if(Input.GetKeyDown(button))
                break;
        }
        ButtonPrompt.SetActive(false);
    }

    public IEnumerator DetectSelect()
    {
        while(true)
        {
            yield return null;
            if(NeoPlayer.Instance.FrontSelected != null && NeoPlayer.Instance.FrontSelected.Type == Constants.Entities.Digger.ID)
                break;
        }
    }

    public IEnumerator DetectMine()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.5f);

            bool isMining = false;

            for(int i = 0; i < NeoPlayer.Instance.FactionEntities.GetLength(0); i++)
            {
                if(NeoPlayer.Instance.FactionEntities[NeoPlayer.Instance.myFactionID, i] == null)
                    continue;

                if(NeoPlayer.Instance.FactionEntities[NeoPlayer.Instance.myFactionID, i].Status 
                == Constants.Entities.Status.Working)
                {
                    isMining = true;
                    break;
                }
            }

            if(isMining)
                break;
        }
    }

    public IEnumerator DetectProximityMine(int faction)
    {
        while(true)
        {
            yield return new WaitForSeconds(0.5f);

            bool hasLandMine = false;

            for(int i = 0; i < NeoPlayer.Instance.FactionEntities.GetLength(0); i++)
            {
                if(NeoPlayer.Instance.FactionEntities[faction, i] == null)
                    continue;

                Debug.Log("DETECTED::: " + NeoPlayer.Instance.FactionEntities[faction, i].Type);

                if(NeoPlayer.Instance.FactionEntities[faction, i].Type == Constants.Entities.ProximityMine.ID)
                {
                    hasLandMine = true;
                    break;
                }
            }

            if(hasLandMine)
                break;
        }
    }

    public IEnumerator DetectCredits(int amount)
    {
        while(true)
        {
            yield return null;
            if(NeoPlayer.Instance.Credits >= amount)
                break;
        }
    }

    public IEnumerator DetectShop()
    {
        ShowHide pMenu = GameObject.FindGameObjectWithTag("PurchaseMenu").GetComponent<ShowHide>();

        while (true)
        {
            yield return null;
            if (pMenu.Shown)
                break;
        }
    }

    public IEnumerator DetectOre()
    {
        while(true)
        {
            yield return null;
            if(NeoPlayer.Instance.FactionEntities[0,0] != null)
                break;
        }
    }

    public IEnumerator DetectEnemy()
    {
        int enemyFactionID = 1;
        if(NeoPlayer.Instance.myFactionID == 1)
            enemyFactionID = 2;

        while(true)
        {
            yield return null;
            if(NeoPlayer.Instance.FactionEntities[enemyFactionID,0] != null)
                break;
        }
    }

    public IEnumerator DetectBaseDamage()
    {
        while (true)
        {
            yield return null;
            if (NeoPlayer.Instance.FactionEntities[NeoPlayer.Instance.myFactionID, 0] != null &&
                NeoPlayer.Instance.FactionEntities[NeoPlayer.Instance.myFactionID, 0].HealthPoints <
                NeoPlayer.Instance.FactionEntities[NeoPlayer.Instance.myFactionID, 0].HealthCapacity)
                break;
        }
    }

}
