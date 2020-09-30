using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamBlack.MoonShot.Networking;
using TeamBlack.Util;
using System;
using EntityStatus = TeamBlack.MoonShot.Constants.Entities.Status; // TODO: constants file

namespace TeamBlack.MoonShot
{
    public abstract class Entity : MonoBehaviour
    {
        // FIXME: Add refrence to NeoPlayer
        public MapManager MapManagerReference => NeoPlayer.Instance.MapManager;
        public float MovementSpeed = 5f;
        // Faction indexers
        public byte factionID;
        public int entityID;

        // Entities have a type that define appearance
        // Appearance communicates behavior
        public byte Type;
        
        // server status of entity
        public EntityStatus Status;

        // Entities exist at a "real" position on the tile map
        public Vector2Int tilePos;

        // Entities can be active or dormant
        // Dormant entities cannot be commanded
        public bool Active;

        // Entities can be hidden in "hammer space"
        // Used for carried entities
        public bool Hidden;

        public byte AttackRange = 0;
        public bool Attacking = true;
        public Vector3 AttackTarget;

        // Basic inventory tracking
        public byte Carried; // TODO: remove?
        public List<byte> Inventory;
        public byte CarryCapacity;

        // Entites have health
        public int HealthPoints = 1;
        public int StartHealth {private set; get;}
        public int HealthCapacity; // Temp

        public Action UpdateListener = () => { };
        
        public virtual void OnUpdate() { }

        public virtual void OnMove(Vector2Int newPos) { }

        public virtual void OnDeath() {
            GameObject.Destroy(this.gameObject);
        }

        public virtual void OnDamage()
        {}

        public virtual void OnHealthChange()
        {}

        public virtual void OnTargetChange()
        { }

        public virtual void OnFull() { }

        public Queue<Util.Job> ToDo = new Queue<Util.Job>();
        private Coroutine _otherDecay;

        public virtual void OnIdle()
        {
            // FIXME: not how its being used, its used as a list of task that happen after another.
            //        maybe have another lsit for diferent things, an on idle list and a seperate todo list
//            if (ToDo.Count != 0) ToDo.Dequeue()();
        }

        protected Util.Job _currJob;
        
        
        public virtual void IsIdle()
        {
            // FIXME: not how its being used, its used as a list of task that happen after another.
            //        maybe have another lsit for diferent things, an on idle list and a seperate todo list
            if ((_currJob == null || _currJob.Status == JobStatus.Done) && ToDo.Count != 0 ) _currJob = ToDo.Dequeue();
            if ((_currJob == null || _currJob.Status == JobStatus.Done)) return;
            _currJob.Update(); 
        }

        public virtual void ForceNextJob()
        {
            //Debug.Log("forcing next job");
            if (ToDo.Count != 0)
            {
                //Debug.Log("has job to force job");
                _currJob = ToDo.Dequeue();
                _currJob.Update();
            }
        }

        // TODO: more specific callbacks available?

        public virtual void OnHidden()
        {
            GameObject.Destroy(this.gameObject);
        }

        public void OnDestroy()
        {
            _currJob = null;
            ToDo.Clear();
            ToDo = null;
            if (NeoPlayer.Instance.Selected.Value != null && NeoPlayer.Instance.Selected.Value.Contains(this)) NeoPlayer.Instance.Selected.Value.Remove(this);
        }
        virtual public void UpdateState(NewPackets.EntityUpdate newState)
        {
            // save original state
            var oldType = Type;
            var oldTilePos = tilePos;
            var oldHidden = Hidden;
            var oldHealthPoints = HealthPoints;
            var oldHealthCapacity = HealthCapacity;
            var oldCarried = Carried;
            var oldCarryCapacity = CarryCapacity;
            var oldStatus = Status;
            var oldAttacking = Attacking;
            var oldTarget = AttackTarget;

            // Copy state
            Type = newState.unitType;
            tilePos = new Vector2Int(newState.position.X, newState.position.Y);
            Hidden = newState.hidden;
            HealthPoints = newState.health;
            HealthCapacity = newState.healthCapacity;
            Carried = (byte)newState.inventory.Count;
            CarryCapacity = newState.carryCapacity;
            Inventory = newState.inventory; // TODO: add oldInventory?
            Status = newState.status;
            AttackTarget = NeoPlayer.Instance.MapManager.WorldPosFromGridIndex(new Vector2Int(newState.attackTarget.X, newState.attackTarget.Y));
            Attacking = newState.attacking;
            AttackRange = newState.attackRange;
            MovementSpeed = (float)newState.moveSpeed;

            if (Attacking)
                Debug.DrawLine(transform.position, AttackTarget, Color.red);

            // called on every update
            OnUpdate();

            // Die first
            if (newState.dead)
                OnDeath();

            if (oldHealthPoints > HealthPoints)
                OnDamage();

            if (oldHealthPoints != HealthPoints)
                OnHealthChange();


            if (oldTarget != AttackTarget || oldAttacking != Attacking)
                OnTargetChange();

            // called if a move occured
            if (oldTilePos != tilePos) OnMove(tilePos);

            if (oldStatus != EntityStatus.Idle && Status == EntityStatus.Idle)
                OnIdle();
            
            if (Status == EntityStatus.Idle)
                IsIdle();

            // TODO: talk about making carried an array
            if (Carried >= CarryCapacity && oldCarried != Carried) OnFull();

            // Hide carried or destroyed entities
            if (Hidden)
                OnHidden();

            if (NeoPlayer.Instance.myFactionID != factionID)
            {
                if (_otherDecay != null)
                    StopCoroutine(_otherDecay);
                _otherDecay = StartCoroutine(Decay());
            }

            UpdateListener();
        }

        private IEnumerator Decay()
        {
            yield return new WaitForSeconds(1);
            Destroy(this.gameObject);
        }
    }
}