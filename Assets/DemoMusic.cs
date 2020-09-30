using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoMusic : MonoBehaviour
{
    public int loopCount;
    public AudioClip[] loops;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DoLoops());
    }

    public IEnumerator DoLoops()
    {
        while(true)
        {
        foreach (AudioClip a in loops)
        {
            GetComponent<AudioSource>().clip = a;
            for(int i = 0; i < loopCount; i++)
            {
                GetComponent<AudioSource>().Play();
                yield return new WaitForSeconds(a.length);
            }
        }
        }
    }
}
