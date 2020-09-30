using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using TeamBlack.MoonShot.Networking;
using TeamBlack.Util;
using System;
using UnityEngine.UI;

namespace TeamBlack.MoonShot
{
    public class Unit : Entity
    {
        [Header("References")]
        public DamagFeedback DamageFeedback;
        public GameObject DeathAftermathPrefab;


        [Header("Stats")]

        private Vector2 _prevPos;
        public Vector2 Velocity { get; private set; }
        public float Speed => Velocity.magnitude;
        private FrameAverage _speedAverage;
        private Animator _myAnimator;
        private Coroutine _job;

        public bool ReturnWhenFull = true;
        public bool ReturnWhenFullOnlyIfHaveOre = true;
        public bool AutoCollect = true;

        private byte? _inventory = null;

        public int Dir => CalculateDir(Velocity.normalized);
        private int CalculateDir(Vector2 unitVector)
        {
            int dir;
            if (Mathf.Abs(unitVector.x) > Mathf.Abs(unitVector.y))
                // left or right
                dir = (unitVector.x > 0) ? 0 : 2;
            else
                // up or down
                dir = (unitVector.y > 0) ? 1 : 3;
            return dir;
        }
        
        #region UnityCallbacks

        private void Awake()
        {
            _prevPos = transform.position;
            _myAnimator = GetComponent<Animator>();
            _speedAverage = new FrameAverage(5);
        }

        private void Update()
        {
            NeoPlayer np = NeoPlayer.Instance;
            
            Vector2 curr = transform.position;
            Velocity = (curr - _prevPos) / Time.deltaTime;
            _prevPos = curr;

            int dir = 0;

            var unit = Velocity.normalized;
            if (Speed > 0.001f)
            {
                if (Mathf.Abs(unit.x) > Mathf.Abs(unit.y))
                {
                    // left or right
                    dir = (unit.x > 0) ? 0 : 2;
                }
                else
                {
                    // up or down
                    dir = (unit.y > 0) ? 1 : 3;
                }
                _myAnimator?.SetInteger("Direction", Dir);
            }
            // _myAnimator?.SetFloat("Speed", Speed);
            _speedAverage.Update(Speed);
            _myAnimator?.SetFloat("Speed", _speedAverage.Average);
        }
        #endregion

        void InteractWithEntity(Entity interact)
        {
            NeoPlayer np = NeoPlayer.Instance;
            
            NewPackets.EntityInteraction entityInteractionPacket;
            entityInteractionPacket.myEntityIndex = entityID;
            entityInteractionPacket.otherFactionIndex = interact.factionID;
            entityInteractionPacket.otherEntityIndex = interact.entityID;

            np.Client.Send(entityInteractionPacket.ByteArray(), NewPackets.PacketType.EntityInteractRequest, (byte)np.myFactionID);
        }
        
        #region EntityCallbacks

        // TODO" fix interp
        public override void OnMove(Vector2Int newPos)
        {

            // FIXME: use real tile size
            Vector2 goal = MapManagerReference.WorldPosFromGridIndex(newPos) + new Vector2(.5f, .5f);
            float maxDist = MovementSpeed / 2;
            if (maxDist < 2.5f) maxDist = 2.5f;
            if (Vector2.Distance(transform.position, goal) > maxDist) // Crappy interp
                transform.position = goal;
            else
                Move(goal);
        }

        public override void OnFull()
        {
            if (ReturnWhenFull)
            {
                var newToDo = new Queue<Util.Job>();
                newToDo.Enqueue(new MoveJob(this, NeoPlayer.Instance.Hub.transform.position));
                newToDo.Enqueue(new InteractJob(this, NeoPlayer.Instance.Hub));
                if (_currJob != null) newToDo.Enqueue(_currJob);
                foreach (var job in ToDo)
                    newToDo.Enqueue(job);
                
                ToDo = newToDo;
                ForceNextJob();
            }
            else if (ReturnWhenFullOnlyIfHaveOre /* and if it has ore in inventory*/ )
            {
                // " "
            }

            GameObject.FindObjectOfType<AudioManager>().PlayCollect();
        }

        public ProjectileFeedback projectileFeedback;

        public override void OnDeath()
        {
            GameObject.FindObjectOfType<AudioManager>().PlayDie();
            GameObject.FindObjectOfType<MusicManager>().RegisterDeath();

            if (DamageFeedback != null)
            {
                DamageFeedback.Die();
            }

            if (DeathAftermathPrefab != null)
            {
                GameObject deathObj = GameObject.Instantiate(DeathAftermathPrefab);
                deathObj.transform.position = transform.position;
                deathObj.transform.Rotate(0,0, UnityEngine.Random.Range(0,360));
            }
            base.OnDeath();
        }

        override public void UpdateState(NewPackets.EntityUpdate newState)
        {
            base.UpdateState(newState);
        }

        public override void OnTargetChange()
        {
            if (projectileFeedback != null)
            {
                if (Attacking)
                {
                    _myAnimator.SetBool("Shooting", true);
                    projectileFeedback.SetTarget(AttackTarget + new UnityEngine.Vector3(0.5f, 0.5f, 0));
                }
                else
                {
                    _myAnimator.SetBool("Shooting", false);
                    projectileFeedback.Hide();
                }
            }
            base.OnTargetChange();
        }

        #endregion

        public void MineArea(HashSet<Vector2Int> area)
        {
            ToDo.Enqueue(new MineJob(this, area));
        }

        public ParticleSystem MineParticles;


        private Coroutine _mineBlockCoroutine; 
        public void AnimateMineBlock(Vector2Int block)
        {
            float time;
            switch (Type)
            {
                case Constants.Entities.Miner.ID:
                    time = Constants.Entities.Miner.ATTACK_INTERVAL;
                    break;
                case Constants.Entities.Digger.ID:
                    time = Constants.Entities.Digger.ATTACK_INTERVAL;
                    break;
                default:
                    time = 0;
                    break;
            }

            int dir = CalculateDir(((Vector2)(block - tilePos)).normalized);
            dir = dir == -1 ? 0 : dir;
            if (_mineBlockCoroutine != null) StopCoroutine(_mineBlockCoroutine);
            _mineBlockCoroutine = StartCoroutine(MineBlockCoroutine(time, dir));

            if (MineParticles != null)
            {
                MineParticles.transform.position = NeoPlayer.Instance.MapManager.WorldPosFromGridIndex(block) + new Vector2(.5f, 1);
                MineParticles.Play();
            }
        }

        private IEnumerator MineBlockCoroutine(float time, int dir)
        {
            _myAnimator.SetInteger("Direction", dir);
            _myAnimator.SetBool("Drilling", true);
            yield return new WaitForSeconds(time + .5f);
            _myAnimator.SetBool("Drilling", false);
        }

        #region Movement
        public void Move(Vector2 waypoint)
        {    
            if (_job != null)
                StopCoroutine(_job);
            _job = StartCoroutine(MoveJob(waypoint));
        }

        private IEnumerator MoveJob(Vector2 waypoint)
        {
            Vector2 start = transform.position;
            float dist = Vector2.Distance(start, waypoint);
            float progress = 0;
            while (Vector2.Distance(waypoint, transform.position) > float.Epsilon)
            {
                transform.position = Vector2.Lerp(start, waypoint, progress);
                yield return null;
                
                progress += MovementSpeed * Time.deltaTime / dist;
            }
        }

        private UnityEngine.Vector3 _waypoint;
        private IEnumerator TestLerp()
        {
            //Vector2 start = transform.position;
            //float dist = Vector2.Distance(start, waypoint);
            //float progress = 0;
            //while (Vector2.Distance(waypoint, transform.position) > float.Epsilon)
            //{
            //    transform.position = Vector2.Lerp(start, waypoint, progress);
            //    yield return null;

            //    progress += MovementSpeed * Time.deltaTime / dist;
            //}
            yield return null;
        }

        public override void OnDamage()
        {
            if (DamageFeedback != null)
            {
                DamageFeedback.PlayImpact();
            }
        }

        public override void OnHealthChange()
        {
            if (DamageFeedback != null)
            {
                DamageFeedback.SetDamageValue((float)HealthPoints/HealthCapacity);
            }
        }
        #endregion
    }
}