using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbientRandomizer : MonoBehaviour
{
    public List<AudioSource> Arps;
    public List<AudioSource> Chords;

    // Start is called before the first frame update
    void Start()
    {
        foreach (AudioSource a in Arps)
        {
            a.volume = 0;
            a.Play();
        }

        foreach (AudioSource a in Chords)
        {
            a.volume = 0;
            a.Play();
        }

        Arps[Random.Range(0, Arps.Count)].volume = 1;
        Chords[Random.Range(0, Arps.Count)].volume = 1;

        StartCoroutine(OnLoop());
    }

    private IEnumerator OnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Arps[0].clip.length);

            foreach (AudioSource a in Arps)
            {
                a.volume = 0;
            }

            foreach (AudioSource a in Chords)
            {
                a.volume = 0;
            }

            Arps[Random.Range(0, Arps.Count)].volume = 1;
            Chords[Random.Range(0, Arps.Count)].volume = 1;
        }
    }
}
