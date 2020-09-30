using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public GameObject selectedObject;
    public GameObject hoveredObject;

    // Start is called before the first frame update
    void Start()
    {
        selectedObject = null;
        hoveredObject = null;
        }

    // Update is called once per frame
    void Update()
    {
        Vector2 ray = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray, Vector2.zero, Mathf.Infinity);
        if (hit)
        {
            GameObject hitObject = hit.transform.root.gameObject;
            if (hoveredObject != hitObject)
            {
                hoveredObject = hitObject;
            }
        }
        else
        {
            hoveredObject = null;
        }
        
        if (Input.GetKey(KeyCode.Mouse0))
        {
            SelectObject(hoveredObject);
        }
    }

    public void SelectObject(GameObject obj)
    {
        if (selectedObject != null)
        {
            if (selectedObject == obj)
            {
                return;
            }
            clearSelection();
        }
        selectedObject = obj;
    }
    public void clearSelection()
    {
        selectedObject = null;
    }
}
