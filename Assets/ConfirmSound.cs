using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ConfirmSound : MonoBehaviour, IPointerDownHandler
{
    private AudioManager _audioManager;
    public void Start()
    {
        _audioManager = GameObject.FindObjectOfType<AudioManager>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _audioManager.PlayUIConfirm();
    }
}