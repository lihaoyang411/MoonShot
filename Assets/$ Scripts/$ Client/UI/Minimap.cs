using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TeamBlack.MoonShot;

public class Minimap : MonoBehaviour, IPointerClickHandler
{
    public GameObject minimapImage;
    [SerializeField] MapManager _myMapManager;
    public void OnPointerClick(PointerEventData eventData) {
        MinimapClick();
    }

    public void MinimapClick()
    {
        var miniMapRect = minimapImage.GetComponent<RectTransform>().rect;
        var screenRect = new Rect(
            minimapImage.transform.position.x, 
            minimapImage.transform.position.y, 
            miniMapRect.width, miniMapRect.height);
        
        var mousePos = Input.mousePosition;
        mousePos.y -= screenRect.y;
        mousePos.x -= screenRect.x;

        var camPos = new Vector3(
            mousePos.x *  (_myMapManager.MapWidth/2 / screenRect.width),
            mousePos.y *  (_myMapManager.MapHeight/2 / screenRect.height),
            Camera.main.transform.position.z);
        Camera.main.transform.position = camPos;
    }
}
