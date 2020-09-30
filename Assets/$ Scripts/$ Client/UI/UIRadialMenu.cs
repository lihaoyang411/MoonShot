using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRadialMenu : MonoBehaviour
{
    public List<Button> btns;
    public float radiusDistance;
    public float angleDeg = -120;
    public bool open;
    void Start() {
        foreach(Button btn in GetComponentsInChildren<Button>(true)) {
            if (btn.name != "MainButton") {
                btns.Add(btn);
            } 
        }
    }
   public void OpenMenu() {
       open = true;
       transform.position = Input.mousePosition;
        float angle = angleDeg / btns.Count * Mathf.Deg2Rad;
        for (int i = 0; i < btns.Count; i++) {
            if (open) {
                float xpos = Mathf.Cos(angle*i) * radiusDistance;
                float ypos = Mathf.Sin(angle*i) * radiusDistance;

                btns[i].transform.position = new Vector2 (transform.position.x + xpos, transform.position.y + ypos);
                btns[i].gameObject.SetActive(true);
            }
           else {
               btns[i].gameObject.SetActive(false);
               btns[i].transform.position = transform.position;
           }
        }
    }
}
