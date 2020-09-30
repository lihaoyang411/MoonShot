using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TeamBlack.MoonShot.Networking;
using System;

namespace TeamBlack.MoonShot
{
    [RequireComponent(typeof(Entity))]
    public class EntitySelector : MonoBehaviour, IPointerClickHandler
    {
        private Entity _entity;
        private NeoPlayer _player => NeoPlayer.Instance;

        //private UnitManager _unitManager => _unit.UnitManager;
        private Action _selectionCallback;
        private void Start()
        {
            _entity = GetComponent<Entity>();

            _selectionCallback = () =>
            {
                var rend = _entity.GetComponent<Renderer>();
                if (_player.Selected.Value.Contains(_entity))
                {
                    rend.material.SetFloat("Vector1_9BA1EEBF", 1); // LOL
                }
                else
                    rend.material.SetFloat("Vector1_9BA1EEBF", 0);
            };

            _player.Selected.Listen(_selectionCallback);
        }

        private void OnDestroy()
        {
            if (_player != null) _player.Selected.UnListen(_selectionCallback);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_entity.factionID == _player.myFactionID && _entity.Active && eventData.button == PointerEventData.InputButton.Left) // Left click for selection
            {
                _player.Selected.Value = new List<Entity>() { _entity };
                Debug.Log($"Selected unit {_entity.entityID}");
            }
        }
    }
}
