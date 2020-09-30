using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PickUpItem : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject inventoryText;
    public int gemCount = 0;
    public int goldCount = 0;
    public AudioSource collectSound;

    void OnTriggerEnter2D(Collider2D other) {

        if(other.tag == "Gem"){
            collectSound.Play();
            gemCount += 1;
            Destroy(other.gameObject);
        }

        if(other.tag == "Gold"){
            collectSound.Play();
            goldCount += 1;
            Destroy(other.gameObject);
        }
        
        
        
    }

    void Update(){
       inventoryText.GetComponent<Text>().text = "Inventory, Gems: " + gemCount ;

    }
}
