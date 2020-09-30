using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TeamBlack.MoonShot;
using TeamBlack.MoonShot.Networking;
/*
[System.Serializable]
public class Purchasable
{
    public Sprite Appearance;
    public int Price;
    public byte ItemID;
    public string Name;

    [TextArea]
    public string Description;
}*/

public class PurchaseMenu : MonoBehaviour
{
    RectTransform pm;
    //public List<Purchasable> objects;
    public List<byte> units;
    public List<byte> items;
    public GameObject unitsPage;
    public GameObject itemsPage;

    public GameObject buttonPrefab;
    public GameObject descriptionText;

    public SpriteBank sb;
    //public GameObject item;
    void Start()
    {
        pm = GetComponent<RectTransform>();
        //container = GameObject.Find("ObjectContainer");
        //RectTransform rt = container.GetComponent<RectTransform>();
        //GridLayoutGroup glg = container.GetComponent<GridLayoutGroup>();
        //rt.sizeDelta = new Vector2(rt.sizeDelta.x, objects.Count * (glg.spacing.y + glg.cellSize.y));
        CreateMenu();
    }

    public void CreateButton(byte itemID, GameObject container) {
        GameObject button = Instantiate(buttonPrefab, container.transform);
        SpriteBank.SpriteMapping purchasable = sb.GetSpriteMapping(itemID);
        foreach (Transform child in button.transform)
        {
            if(child.name == "NameLabel" ) {
                Text name = child.GetComponent<Text>();
                name.text = purchasable.Name;
            }
            else if(child.name == "PriceLabel" ) {
                Text price = child.GetComponent<Text>();
                price.text = "$"+purchasable.Price.ToString();
            }
            else if(child.name == "Icon" ) {
                Image icon = child.GetComponent<Image>();
                icon.sprite = purchasable.Image;
            }
            else if(child.name == "DescriptionLabel" ) {
                Text description = child.GetComponent<Text>();
                description.text = purchasable.Description;
            }
        } 
        button.GetComponent<Button>().onClick.AddListener(
            () => {
                Debug.Log($"Trying to buy entity type {purchasable.ItemID} for {purchasable.Price} credits...");

                NeoPlayer player = GameObject.FindGameObjectWithTag("Player").GetComponent<NeoPlayer>();

                NewPackets.Buy buyPacket = new NewPackets.Buy();
                buyPacket.type = purchasable.ItemID;

                player.Client.Send(buyPacket.ByteArray(), NewPackets.PacketType.Buy, (byte)player.myFactionID);
            });
        button.GetComponent<PurchaseButton>().descriptionText = descriptionText.GetComponent<Text>();         
    }
    public void CreateMenu() {
        RectTransform rt = GetComponent<RectTransform>();
        //float xspacing = rt.rect.width/(objects.Count+1);
        for (int i = 0; i < units.Count; i++) {
            CreateButton(units[i], unitsPage);
        }
        for (int j = 0; j < items.Count; j++) {
            CreateButton(items[j], itemsPage);
        }
    }
}
