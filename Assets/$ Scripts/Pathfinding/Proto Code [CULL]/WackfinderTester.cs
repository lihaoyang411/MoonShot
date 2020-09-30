using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WackfinderTester : MonoBehaviour
{
    public Vector2 from;
    public Vector2 to;

    public GameObject wall;
    public GameObject ground;
    public GameObject destMarker;
    public GameObject startMarker;
    public byte[,] testMap;

    public void Start() {
        
        testMap = new byte[256,256];
        for(int y = 0; y < 256; y++)
        {
            for(int x = 0; x < 256; x++)
            {
                testMap[y, x] = 0;
                if(Random.Range(0,2) == 0 && y != 50 && x != 50) testMap[y, x] = 1;

                if(testMap[y, x] == 0)
                {
                    GameObject.Instantiate(ground).transform.position = new Vector3(x, y, 10);
                }
                else
                {
                    GameObject.Instantiate(wall).transform.position = new Vector3(x, y, 10);
                }
            }
        }

        from = new Vector2(50, 50);
        to = new Vector2(Random.Range(0, 256), Random.Range(0, 256));

        GameObject.Instantiate(destMarker).transform.position = new Vector3(to.x, to.y, -9);
        GameObject.Instantiate(startMarker).transform.position = new Vector3(from.x, from.y, -9);

        GetComponent<Wackfinder>().WackfinderINIT(
            testMap,
            new Wackfinder.Coord2D((int)from.x, (int)from.y),
            new Wackfinder.Coord2D((int)to.x, (int)to.y)
        );

    }
}
