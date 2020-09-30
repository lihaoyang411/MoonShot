using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamBlack.MoonShot;

public class TempShootPlayer : MonoBehaviour
{
    public AudioSource AttackSound;
    public Sprite[] MuzzleFlashes;
    public SpriteRenderer[] FlashDisplays;

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<Unit>().Attacking
        && !AttackSound.isPlaying)
            AttackSound.Play();

        if (AttackSound.isPlaying)
        {
            foreach (SpriteRenderer s in FlashDisplays)
            {
                s.enabled = true;
                s.sprite = MuzzleFlashes[Random.Range(0, MuzzleFlashes.Length)];
            }
        }
        else {
            foreach (SpriteRenderer s in FlashDisplays)
            {
                s.enabled = false;
            }
        }
    }
}
