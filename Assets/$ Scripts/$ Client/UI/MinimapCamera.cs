using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamBlack.MoonShot;
public class MinimapCamera : MonoBehaviour
{
    Camera c;
    [SerializeField] MapManager _myMapManager;
    // Start is called before the first frame update
    void Start()
    {
        c = GetComponent<Camera>();
        c.orthographicSize = _myMapManager.MapWidth/2; // Assumes map is a square, MapWidth and MapHeight are equal
    }
}
