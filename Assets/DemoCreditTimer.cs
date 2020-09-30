using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemoCreditTimer : MonoBehaviour
{
    public int seconds;
    public Text text;
    public Text creditText;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            seconds--;
            if (seconds < 1)
            {
                text.text = "FINAL SCORE " + creditText.text;
            }
            else
            {
                text.text = $"Time Remaining: [{seconds}]";
            }
        }
    }
}
