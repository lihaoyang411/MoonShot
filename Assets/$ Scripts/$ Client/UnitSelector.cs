using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TeamBlack.MoonShot
{
    [RequireComponent(typeof(Unit))]
    public class UnitSelector : MonoBehaviour
        //, IPointerClickHandler 
    {
        //private Unit _unit;

        ////private UnitManager _unitManager => _unit.UnitManager;
        
        //private void Start()
        //{
        //    _unit = GetComponent<Unit>();
        //    var rend = _unit.GetComponent<Renderer>();
        //    _unit.UnitManager.Selected.Listen(() => {
        //        if (_unit == _unit.UnitManager.Selected.Value) 
        //        {
        //            rend.material.SetFloat("_selected", 1);
        //        }
        //        else 
        //        {
        //            rend.material.SetFloat("_selected", 0);
        //        }
        //    });
        //}

        //public void OnPointerClick(PointerEventData eventData)
        //{
        //    if (_unit.PlayerIndex == _unit.UnitManager.Player.PlayerNumber)
        //    {
        //        _unit.UnitManager.Selected.Value = _unit;
        //        // _unitManager.Selected.Value.GetComponent<Renderer>().material.SetFloat("_selected", 1);
        //        Debug.Log($"Selected unit {_unit.Index}");
        //    }
        //}
    }
}

