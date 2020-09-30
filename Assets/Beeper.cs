using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamBlack.MoonShot;

public class Beeper : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (GetComponent<ProximityMine>().Active)
        {
            StartCoroutine(Beep());
            StartCoroutine(DelayedDeath()); // Just to make sure it doesn't beep forever
        }
    }

    private IEnumerator DelayedDeath()
    {
        yield return new WaitForSeconds(20);
        GameObject.Destroy(this.gameObject);
    }

    private IEnumerator Beep()
    {
        float interval = 1f;
        float min = 0.1f;

        while (true)
        {

            if (interval > min)
                interval -= 0.1f;
            else
                interval = min;

            yield return new WaitForSeconds(interval);

            GetComponent<AudioSource>().Play();
        }
    }
}
