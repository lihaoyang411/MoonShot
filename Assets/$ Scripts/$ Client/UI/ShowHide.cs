using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowHide : MonoBehaviour
{  
    Animator anim;
    public bool Shown = false;
    private void Start() {
        anim = GetComponent<Animator>();
        anim.Play("Close", -1, 1);
    }
    public void ToggleMenu() {
        //bool isOn = gameObject.activeSelf;
        if (!anim.GetBool("Open")) {
            anim.SetBool("Open", true);
            Shown = false;
            //gameObject.SetActive(!isOn);
        } else {
            anim.SetBool("Open", false);
            Shown = true;
        }
        //gameObject.SetActive(!isOn);
    }
}
