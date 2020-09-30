using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ScrollSound : MonoBehaviour, IPointerEnterHandler
{
    private AudioManager _audioManager;
    public void Start()
    {
        _audioManager = GameObject.FindObjectOfType<AudioManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _audioManager.PlayUIScroll();
    }
}