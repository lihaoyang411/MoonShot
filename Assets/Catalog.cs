using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamBlack.MoonShot;

public class Catalog : ScriptableObject
{
    public Sprite NullSprite;

    [System.Serializable]
    public class Item
    {
        public string Name;
        public byte ItemID;
        public int Price;

        [TextArea]
        public string Description;
        public Sprite F1Appearance; // Default
        public Sprite F2Appearance; // Set only if item differs for faction 2
    }

    public List<Item> items;

    private Item[] _itemMap;

    private void Awake()
    {
        _itemMap = new Item[256];

        foreach (Item i in _itemMap)
        {
            _itemMap[i.ItemID] = i;
        }
    }

    public Sprite GetAppearance(int i)
    {
        Item toReturn = _itemMap[i];

        if (toReturn.F1Appearance == null && toReturn.F2Appearance == null)
        {
            return NullSprite;
        }

        if (NeoPlayer.Instance.myFactionID == 2 && toReturn.F2Appearance != null)
        {
            return toReturn.F2Appearance;
        }

        return toReturn.F1Appearance;
    }
}
