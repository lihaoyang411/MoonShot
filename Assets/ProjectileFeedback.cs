using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileFeedback : MonoBehaviour
{
    public float Offset;
    public GameObject TargetSelector;
    public ParticleSystem Particles;
    public AudioSource ShootSound;

    public void Awake()
    {
        TargetSelector.transform.SetParent(null);
    }

    private void Update()
    {
        if (Particles.isPlaying)
        {
            Vector3 dir = TargetSelector.transform.position - transform.position;
            Particles.transform.position = transform.position + dir.normalized * Offset;
        }
    }

    public void SetTarget(Vector3 target)
    {
        TargetSelector.transform.position = target;
        Particles.Play();
        ShootSound.Play();
    }

    public void Hide()
    {
        Particles.Stop();
        ShootSound.Stop();
    }

    private void OnDestroy()
    {
        GameObject.Destroy(TargetSelector);
    }
}
