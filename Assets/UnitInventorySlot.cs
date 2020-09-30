using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using UnityEngine.EventSystems;
using TeamBlack.MoonShot.Networking;
using TeamBlack.MoonShot;

public class UnitInventorySlot : MonoBehaviour, IPointerClickHandler
{
    private bool _initialized;
    private int _ownerID;
    private byte _invnentoryID;

    
    
    public void SetItem(int ownerID, byte inventoryID, byte type)
    {
        transform.GetChild(0).GetComponent<Image>().sprite = GetComponentInParent<SpriteBank>().GetSprite(type);
        _ownerID = ownerID;
        _invnentoryID = inventoryID;

        _initialized = true;
    }

    public void OnPointerClick(PointerEventData ped)
    {
        FindObjectOfType<CameraMovement>().DisableSelectForFrame();
    }

    public void TryDrag()
    {
        if (!_initialized)
            return;

        //StartCoroutine(Drag());
        RequestDeploy();
        GetComponentInParent<UnitInventory>().HideInventory();
    }

    private IEnumerator Drag()
    {
        while (!Input.GetMouseButtonDown(0))
        {
            transform.GetChild(0).transform.position = Input.mousePosition; // I can't believe this works lol
            yield return null;
        }

        transform.GetChild(0).transform.localPosition = Vector3.zero;
        RequestDeploy();
        GetComponentInParent<UnitInventory>().HideInventory();
        //NeoPlayer.Instance.Selected.Value = new List<TeamBlack.MoonShot.Entity> { NeoPlayer.Instance.FactionEntities[NeoPlayer.Instance.myFactionID, _ownerID] };
    }

    private void RequestDeploy()
    {
        NewPackets.Deploy deployPacket;
        deployPacket.InventoryID = _invnentoryID;
        deployPacket.EntityID = _ownerID;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<NetClient>().Send(deployPacket.ByteArray(), NewPackets.PacketType.Deploy, (byte)player.GetComponent<NeoPlayer>().myFactionID);
    }

}
