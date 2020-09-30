using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PurchaseButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Text descriptionText;
    public Text text;
    public void OnPointerEnter(PointerEventData eventData) {
        descriptionText.text = text.text;
    }
    public void OnPointerExit(PointerEventData eventData) {
        descriptionText.text = "";
    }
}
