using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TeamBlack.MoonShot;
using System;

public class UnitInventory : MonoBehaviour
{
    public GameObject InventorySlotPrefab;
    public float InitialSpacing;
    public float HorizontalSpacing;
    public float VerticalSpacing;
    public float SlotSize;
    SpriteBank spriteBank;
    Action display;
    Entity selected;
    public List<GameObject> groupSelected;
    public GameObject groupSelectedPrefab;
    
    public GameObject selectionUI;
    public GameObject groupSelect;
    public void Start()
    {
        var np = NeoPlayer.Instance;
        spriteBank = GetComponent<SpriteBank>();
        NeoPlayer.Instance.Selected.Listen(() =>
        {
            // When unit is not selected
            if (np.FrontSelected == null) {
                HideSelected();
                return;
            }
            // When only one unit is selected
            else if(np.Selected.Value != null && np.Selected.Value.Count == 1){
                HideSelected();
                DisplayInventory(np.FrontSelected.entityID, np.FrontSelected.Inventory.ToArray());
                if (selected != np.FrontSelected){
                    selected = np.FrontSelected;
                    if (selected != null) {
                        DisplaySelected(selected);
                        display = () => 
                                DisplaySelected(selected);
                        
                        if (!checkActionExists()){
                            selected.UpdateListener += display;
                        }
                    }
                }
                print("Selected" + selected);
                selectionUI.SetActive(true);
                
                
            }
            // When multiple units are selected
            else if(np.Selected.Value != null && np.Selected.Value.Count > 1){
                HideSelected();
                Debug.Log("multiple selected");
                foreach(Entity e in np.Selected.Value){
                    //GameObject unitSelected = Instantiate(groupSelectedPrefab, groupSelect.transform);
                    groupSelected.Add(CreateIcon(e));
                    //groupSelected.Add(unitSelected);
                }
                groupSelect.SetActive(true);
            }
            });
    }
    GameObject CreateIcon(Entity unit) {
        GameObject unitSelected = Instantiate(groupSelectedPrefab, groupSelect.transform);
         foreach(Transform child in unitSelected.transform) {
                if(child.name == "Icon")
                    child.GetComponent<Image>().sprite = spriteBank.GetSprite(unit.Type);
         }
        return unitSelected;
    }
    void ClearList(){
        if(groupSelected != null){
            foreach(GameObject g in groupSelected) {
                Destroy(g);
            }
            groupSelected.Clear();
        }
    }
    bool checkActionExists() {
        foreach (Action a in selected.UpdateListener.GetInvocationList()){
            if (a == display) {
                return true;
            }
        }
        return false;
    }
    public void HideSelected() {
        //print("hide:" + selected);
        if (selected != null) {
            selected.UpdateListener -= display;
        }
        ClearList();
        selectionUI.SetActive(false);
        groupSelect.SetActive(false);
    }
    public void DisplaySelected(Entity unit) {
        if (unit != null)
        {
            foreach(Transform child in selectionUI.transform) {
                if(child.name == "Icon") {
                        child.GetComponent<Image>().sprite = spriteBank.GetSprite(unit.Type); 
                }
                else if (child.name == "HealthText") {
                    string hpText = unit.HealthPoints + " / " + unit.HealthCapacity.ToString();
                    child.GetComponent<Text>().text = hpText;
                }
                else if (child.name == "StatusText") {
                    child.GetComponent<Text>().text = unit.Status.ToString();
                }
            }
        }
    }
    public void HideInventory()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    public void DisplayInventory(int unitIndex, byte[] inventory)
    {

        HideInventory();
        StopAllCoroutines();

        float curX = InitialSpacing;
        float curY = VerticalSpacing / 2;

        for (int i = 0; i < inventory.Length; i++)
        {
            UnitInventorySlot inventorySlot;
            if (i >= transform.childCount)
                inventorySlot = GameObject.Instantiate(InventorySlotPrefab).GetComponent<UnitInventorySlot>();
            else
            {
                transform.GetChild(i).gameObject.SetActive(true);
                inventorySlot = transform.GetChild(i).GetComponent<UnitInventorySlot>();
            }

            inventorySlot.transform.SetParent(transform);
            inventorySlot.transform.localPosition = new Vector3(curX, curY, 0);
            
            // Better for scaling? but doesn't change children
            //inventorySlot.GetComponent<RectTransform>().sizeDelta = new Vector2 (SlotSize, SlotSize); 

            inventorySlot.transform.localScale = new Vector3(SlotSize, SlotSize, SlotSize);

            inventorySlot.SetItem(unitIndex, (byte)i, inventory[i]);

            if(curY < 0)
                curX += HorizontalSpacing;
            curY *= -1;
        }

        StartCoroutine(TempScanInventory(unitIndex));
    }

    private IEnumerator TempScanInventory(int unitIndex)
    {
        Entity toScan = NeoPlayer.Instance.FactionEntities[NeoPlayer.Instance.myFactionID, unitIndex];
        int curSize = toScan.Inventory.Count;
        while (true)
        {
            if (toScan == null)
            {
                HideInventory();
                break;
            }

            if (curSize != toScan.Inventory.Count)
            {
                DisplayInventory(unitIndex, toScan.Inventory.ToArray());
            }

            yield return null;
        }
    }
}
