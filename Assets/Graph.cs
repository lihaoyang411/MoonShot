using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Graph : MonoBehaviour
{
    [SerializeField]
    private Sprite circleSprite;
    public List<int> values;
    public RectTransform graphContainer;
    public Font font;

    private void Start() {
        ShowGraph(values);
    }
    private void Update() {
        
    }
    private GameObject CreateCircle (Vector2 anchoredPos) {
        GameObject circleObject = new GameObject("point", typeof(Image));
        circleObject.transform.SetParent(graphContainer);
        circleObject.GetComponent<Image>().sprite = circleSprite;
        RectTransform rt = circleObject.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(10, 10);
        return circleObject;
    }

    private void ShowGraph(List<int> values) {
        RectTransform rt = GetComponent<RectTransform>();
        float ymax = 100f;
        float graphHeight = rt.sizeDelta.y;
        float xspacing  = rt.rect.width/(values.Count-1);
        Debug.Log(graphHeight);
        for (int i = 0; i < values.Count; i++) {
            float xpos = xspacing * i;
            float ypos = values[i]*graphHeight/ymax;
            GameObject point = CreateCircle(new Vector2(xpos, ypos));
            // Create Price Text
            Text priceText = new GameObject("price", typeof(Text)).GetComponent<Text>();
            priceText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,0);
            priceText.transform.SetParent(point.transform);
            priceText.font = font;
            priceText.fontSize = 12;
            priceText.text = "$"+values[i];

        }
    }
}
