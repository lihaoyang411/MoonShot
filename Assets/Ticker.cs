using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamBlack.MoonShot;
using UnityEngine.Experimental.Rendering.LWRP;

public class Ticker : MonoBehaviour
{

    public float StartInterval = 1f;
    public float EndInterval = 0.1f;
    public float FuseTime = 1;

    private AudioManager _audioManager;
    private Coroutine _flashLight;

    public Light2D FeedbackLight;
    public Entity Owner;

    // Start is called before the first frame update
    void Start()
    {
        _audioManager = GameObject.FindObjectOfType<AudioManager>();
        if(Owner.Status == Constants.Entities.Status.Deployed)
            StartCoroutine(TickTick());
    }

    private IEnumerator TickTick()
    {
        float currentInterval = StartInterval;
        float elapsed = 0;

        while (true)
        {
            _audioManager.PlayRatchet();

            if (_flashLight != null) StopCoroutine(_flashLight);
            StartCoroutine(FlashLight());

            elapsed += currentInterval;

            yield return new WaitForSeconds(currentInterval);

            currentInterval = Mathf.Lerp(StartInterval, EndInterval, elapsed/FuseTime);
        }
    }

    private IEnumerator FlashLight()
    {
        FeedbackLight.intensity = 1;
        while (true)
        {
            yield return new WaitForEndOfFrame();
            FeedbackLight.intensity -= Time.deltaTime;
            if (FeedbackLight.intensity < 0.5f)
            {
                FeedbackLight.intensity = 0.5f;
                break;
            }
        }
    }
}
