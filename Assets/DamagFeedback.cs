using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamagFeedback : MonoBehaviour
{
    public GameObject HealthContainer;
    public Image HealthBar;

    public Vector2 SmokeRange;
    public Vector2 PuddleRange;

    public ParticleSystem ImpactParticles;
    public ParticleSystem SmokeParticles;
    public ParticleSystem PuddleParticles;

    public float StopDelay;
    public float DeathDelay;

    public AudioSource DamageSound;

    public void PlayImpact()
    {
        DamageSound.Play();
        ImpactParticles.Play();
        SmokeParticles.Play();
    }

    public void SetDamageValue(float percent)
    {
        HealthBar.fillAmount = percent;

        var emission = SmokeParticles.emission;
        emission.rateOverTime = Mathf.Lerp(SmokeRange.x, SmokeRange.y, 1 - percent);

        var emission2 = PuddleParticles.emission;
        emission2.rateOverTime = Mathf.Lerp(PuddleRange.x, PuddleRange.y, 1 - percent);
    }

    public void Die()
    {
        transform.SetParent(null);
        StartCoroutine(DelayedDeath());
    }

    private IEnumerator DelayedDeath()
    {
        GameObject.Destroy(HealthContainer);
        yield return new WaitForSeconds(StopDelay);
        SmokeParticles.Stop();
        PuddleParticles.Stop();
        ImpactParticles.Stop();
        yield return new WaitForSeconds(DeathDelay);
        GameObject.Destroy(gameObject);
    }
}
