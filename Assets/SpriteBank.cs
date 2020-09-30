using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteBank : MonoBehaviour
{
    [System.Serializable]
    public class SpriteMapping
    {
        public byte ItemID;
        public Sprite Image;
        public string Name;
        public int Price;
        [TextArea]
        public string Description;
    }
    public Sprite NullSprite;
    public List<SpriteMapping> SpriteMappings;
    private Sprite[] _spriteMap;

    private void Awake()
    {
        _spriteMap = new Sprite[256];

        foreach (SpriteMapping m in SpriteMappings)
        {
            _spriteMap[m.ItemID] = m.Image;
        }
    }

    public Sprite GetSprite(byte itemID)
    {
        if (_spriteMap[itemID] == null)
            return NullSprite;
        else
            return _spriteMap[itemID];
    }

    public SpriteMapping GetSpriteMapping(byte itemID) 
    {
        foreach (SpriteMapping m in SpriteMappings)
        {
            if (m.ItemID == itemID) 
            {
                return m;
            }
        }
        return null;
    }
}
