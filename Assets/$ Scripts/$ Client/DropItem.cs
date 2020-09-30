using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace TeamBlack.MoonShot
{
    public class DropItem : MonoBehaviour
    {
        [SerializeField]
        static List<GameObject> instances = new List<GameObject>();
        //[SerializeField] private List<OreTile> _wallTiles;
        //public Vector2 itemPosition;
        [SerializeField]
        static GameObject gem;
        //public GameObject gold;
        
        
        void Start(){
            if (gem == null){
                gem = Resources.Load("Gem_Test") as GameObject;
            }
        }

        public static void Drop(Vector2 itemPosition, int Ore){
            
            if (Ore == 2){
                instances.Add(Instantiate(gem, itemPosition, Quaternion.identity));
            }
            
            
            // //if ((from Unit script for which material it is) == gold){
            //     instances.Add(Instantiate(gold, itemPosition));
            // }
            
        }
    }
}
