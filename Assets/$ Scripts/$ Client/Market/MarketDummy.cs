using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketDummy : MonoBehaviour
{
    public static int marketCap = 100;
    public static int marketMin = 1;

    [System.Serializable]
    public struct Commodity
    {
        public string name;
        public int count;

        public int value;
    }

    [Header("Debug Settings")]

    public Commodity[] initCommodities;
    public int baseVal;

    public Commodity[] commodities; // Should be private

    private void Start()
    {
        InitMarket(baseVal, initCommodities);
    }

    public void InitMarket(int baseValue, Commodity[] countedCommodities)
    {
        commodities = countedCommodities;
        

        // Count the total commodities for value ratio

        int totalCommodityCount = 0;

        foreach (Commodity c in commodities)
        {
            if (c.name == "")
                print("ERROR: commodity should have a name");

            if (c.count == 0)
                print("ERROR: commodity count should not be zero");

            totalCommodityCount += c.count;
        }

        // Set the starting commodity values based on prevalence

        for(int i = 0; i < commodities.Length; i++)
        {
            commodities[i].value = 
                (int) (baseValue * (1 - ((float)commodities[i].count / (float)totalCommodityCount)));
        }

    }
}
