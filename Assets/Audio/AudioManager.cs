using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TeamBlack.MoonShot;

public class AudioManager : MonoBehaviour
{
    [Header("UI Clips")]
    public AudioClip[] RatchetClips;

    [Header("Economy Clips")]
    public AudioClip UIConfirm;
    public AudioClip UIScroll;
    public AudioClip BuySound;
    public AudioClip SellSound;

    [Header("Selection Clips")]
    public AudioClip UnitSelectSound;
    public AudioClip MultiUnitSelectSound;
    public AudioClip TileSelectSound;
    public AudioClip[] TileConfirmSounds;

    [Header("Diagetic Clips")]
    public AudioClip CollectSound;
    public AudioClip DeploySound;
    public AudioClip SandbagDeploySound;
    public AudioClip DeathSound;
    public AudioClip DigSound;

    [Header("Audio Sources")]
    public AudioSource EconomySounds;
    public AudioSource SelectSounds;
    public AudioSource CollectionSounds;
    public AudioSource DeathSounds;
    public AudioSource RatchetSounds;

    private void Start()
    {
        try
        {
        Action selectionCallback = () =>
        {
            if (NeoPlayer.Instance.Selected.Value.Count == 1)
            {
                PlayUnitSelect();
            }
            else if (NeoPlayer.Instance.Selected.Value.Count > 1)
            {
                PlayMultiUnitSelect();
            }
               
        };

        NeoPlayer.Instance.Selected.Listen(selectionCallback);
        }
        catch(Exception e)
        {
            Debug.Log("AUDIO MANAGER COULDN'T FIND PLAYER");
        }
    }

    // Non-diagetic
    public void PlayUIScroll()
    {
        SelectSounds.PlayOneShot(UIScroll);
    }
    public void PlayUIConfirm()
    {
        SelectSounds.PlayOneShot(UIConfirm);
    }
    public void PlayUnitSelect()
    {
        SelectSounds.PlayOneShot(UnitSelectSound);
    }
    public void PlayMultiUnitSelect()
    {
        SelectSounds.PlayOneShot(MultiUnitSelectSound);
    }
    public void PlayTileSelect()
    {
        SelectSounds.PlayOneShot(TileSelectSound);
    }
    public void PlayTileConfirm()
    {
        SelectSounds.PlayOneShot(TileConfirmSounds[UnityEngine.Random.Range(0,TileConfirmSounds.Length)]);
    }

    public void PlayBuy()
    {
        SelectSounds.PlayOneShot(BuySound);
    }
    public void PlaySell()
    {
        SelectSounds.PlayOneShot(SellSound);
    }

    public void PlayRatchet()
    {
        RatchetSounds.PlayOneShot(RatchetClips[UnityEngine.Random.Range(0, RatchetClips.Length)]);
    }

    // ?? Diagetic
    public void PlayExplosion()
    { }
    public void PlayHurt()
    { }

    public void PlayDie()
    {
        DeathSounds.PlayOneShot(DeathSound);
    }

    public void PlayCollect()
    {
        CollectionSounds.PlayOneShot(CollectSound);
    }
    public void PlayDeploy()
    {
        CollectionSounds.PlayOneShot(DeploySound);
    }

    public void PlaySandbagDeploy()
    {
        CollectionSounds.PlayOneShot(SandbagDeploySound);
    }

    public void PlayDig()
    {
        CollectionSounds.PlayOneShot(DigSound);
    }
}
