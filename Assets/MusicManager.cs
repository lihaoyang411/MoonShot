using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{

    public AudioSource player;
    public AudioMood[] Moods;
    public bool _encountering;
    public bool _combatting;

    public int CurrentMood = -1;

    private Coroutine Randomizer;

    [System.Serializable]
    public class AudioMood
    {
        public AudioClip[] Clips;
        public AudioClip Intro;
        public AudioClip Outro;
    }

    private int deaths;
    public void RegisterDeath()
    {
        deaths++;
        if (deaths > 0)
            _encountering = true;
        if (deaths > 10)
            _combatting = true;
    }

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        StartCoroutine(IntroSwitch());
    }

    private IEnumerator IntroSwitch()
    {
        CurrentMood++;

        player.PlayOneShot(Moods[CurrentMood].Intro);


        yield return new WaitForSeconds(Moods[CurrentMood].Intro.length);
        Randomizer = StartCoroutine(Randomize());
    }

    private IEnumerator OutroIntroSwitch()
    {
        player.PlayOneShot(Moods[CurrentMood].Outro);

        yield return new WaitForSeconds(Moods[CurrentMood].Intro.length);
        CurrentMood++;

        player.PlayOneShot(Moods[CurrentMood].Intro);

        yield return new WaitForSeconds(Moods[CurrentMood].Intro.length);
        Randomizer = StartCoroutine(Randomize());
    }

    private IEnumerator Randomize()
    {
        while (true)
        {
            if (!player.isPlaying)
            {
                if (_encountering && CurrentMood < 1)
                {
                    StartCoroutine(OutroIntroSwitch());
                    if(Randomizer != null) // ??
                        StopCoroutine(Randomizer);
                    break;
                }
                else if (_encountering && _combatting && CurrentMood < 2)
                {
                    StartCoroutine(OutroIntroSwitch());
                    if (Randomizer != null) // ??
                        StopCoroutine(Randomizer);
                    break;
                }
                else
                    player.PlayOneShot(Moods[CurrentMood].Clips[Random.Range(0, Moods[CurrentMood].Clips.Length)]);
            }
            yield return new WaitForEndOfFrame();
        }
    }
}
