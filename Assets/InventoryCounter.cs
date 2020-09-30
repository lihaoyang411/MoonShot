using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TeamBlack.MoonShot;

// This is just for debug purposes, should use delegate or whatever
public class InventoryCounter : MonoBehaviour
{
    public Text t;
    public Entity e;

    // Update is called once per frame
    void Update()
    {
        t.text = $"[{e.Carried}/{e.CarryCapacity}]";
    }
}
