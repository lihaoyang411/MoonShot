using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapScreenIndicator : MonoBehaviour
{
    SpriteRenderer sr;
    public Camera mainCamera;
    void Start() {
        sr = GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Sliced;
    }
    void FixedUpdate() {
        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;
        sr.size = new Vector2(width, height);
    }
}
