using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveableUI : MonoBehaviour
{
    Vector3 offset;
    public GameObject menu;
    RectTransform rt;
    float minX;
    float maxX;
    float minY;
    float maxY;
    private void Start() {
        rt = menu.GetComponent<RectTransform>();
        minX = rt.sizeDelta.x/2;
        minY = rt.sizeDelta.y/2;
        maxX = Screen.width - rt.sizeDelta.x/2;
        maxY = Screen.height - rt.sizeDelta.y/2;

    }
    public void BeginDrag()
    {
        offset = menu.transform.position - Input.mousePosition;
    }

    public void OnDrag() 
    {
        menu.transform.position = new Vector3 (
            Mathf.Clamp(Input.mousePosition.x + offset.x, minX, maxX),
            Mathf.Clamp(Input.mousePosition.y + offset.y, minY, maxY));
    }
}
