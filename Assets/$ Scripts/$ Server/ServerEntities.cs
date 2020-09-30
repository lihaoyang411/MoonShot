using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TeamBlack.MoonShot.Networking
{

    ////////////////////////////////////////////////////////
    ////                                                ////
    ////                   1. CONTEXT                   ////
    ////                                                ////
    ////////////////////////////////////////////////////////


    // A global reference object that
    // allows entities & jobs to "know where they are"
    // This is important for affecting the game state
    public struct Context
    {

        public int ownerID;                     // Entity indexer within faction
        public int factionID;                   // Faction indexer within game
        public ServerGameManager gameManager;   // The instance of the game

        public Context(int ownerID, int factionID, ServerGameManager context)
        {
            this.ownerID = ownerID;
            this.factionID = factionID;
            this.gameManager = context;
        }

        public ServerMapManager MapContext()
        {
            return gameManager.MapManager;
        }

        public ServerFactionManager FactionContext()
        {
            return gameManager.PlayerFactions[factionID];
        }

        public Entity OwnerContext()
        {
            return FactionContext().Entities[ownerID];
        }

        public IWorker WorkerContext()
        {
            IWorker worker = OwnerContext() as IWorker;
            if (worker == null)
                Debug.Log("SERVER ERROR: not a worker!");

            return worker;
        }

        public IMover MoverContext()
        {
            IMover mover = OwnerContext() as IMover;
            if (mover == null)
                Debug.Log("SERVER ERROR: not a mover!");

            return mover;
        }
    }


    /////////////////////////////////////////////////////
    ////                                             ////
    ////                   2. JOBS                   ////
    ////                                             ////
    /////////////////////////////////////////////////////


    // An action that an entity is currently taking
    // Jobs are stepped on the core game loop to advance game state
    // Jobs are the main units that affect the game state
    public abstract class Job
    {

        // Every job has a context within the game
        protected Context _context;

        public Job(Context context)
        {
            _context = context;
        }

        // Progress of the job updated according to time step
        public abstract void Step();
    }

    // For now, all commandable entities have these behaviors

    public class AttackCooldownJob : Job
    {
        // TODO: const file?
        private double _seconds;

        public AttackCooldownJob(double seconds, Context context) : base(context)
        {
            _seconds = seconds;
        }

        public override void Step()
        {
            _seconds -= _context.gameManager.DeltaTime;

            if (_seconds <= 0)
            {
                IAttacker owner = _context.OwnerContext() as IAttacker;

                if (owner != null)
                    owner.ResetAttack();
                else
                    Debug.Log("ERROR: non-attacker has CD job");
            }
        }
    }

    public class MoveJob : Job
    {
        private ByteVector2[] _path;
        private double _tilesComplete = 0;
        private int _fullTilesComplete = 0;
        private IMover _mover;

        public MoveJob(ByteVector2[] path, Context context) : base(context)
        {
            _path = path;
            _mover = context.OwnerContext() as IMover;
            if (_mover == null)
            {
                Debug.Log("ERROR: MoveJob assigned to non mover");
                context.WorkerContext().ClearJob();
            }
        }

        public override bool Equals(object otherMoveJob)
        {
            if (otherMoveJob.GetType() != typeof(MoveJob))
                return false;

            MoveJob other = (MoveJob)otherMoveJob;

            if (other._path[other._path.Length - 1]
            == _path[_path.Length - 1])
            {
                return true;
            }
            return false;
        }

        public override void Step()
        {
            // Base case
            if (_fullTilesComplete >= (_path.Length - 1))
            {
                _context.WorkerContext().ClearJob();
                return;
            }

            // 1 speed ~= 1 tile/second
            _tilesComplete += _mover.GetSpeed() * _context.gameManager.DeltaTime;

            // When a "full step is completed"
            if ((int)_tilesComplete > _fullTilesComplete)
            {
                // Update "full step" progress tracker
                _fullTilesComplete = (int)_tilesComplete;

                // Prevent "over step" edge case
                if (_fullTilesComplete >= _path.Length)
                    _fullTilesComplete = _path.Length - 1;

                // Update unit state
                _context.OwnerContext().SetPosition(_path[_fullTilesComplete]);
            }
        }
    }

    public class DigJob : Job
    {
        // TODO: constants file?
        private ByteVector2 _tile;
        private float _mineTime = .5f;
        private bool _mine = false;

        private double _progress = 0;

        public DigJob(ByteVector2 tile, float time, bool mine, Context context) : base(context)
        {
            _tile = tile;
            _mineTime = time;
            _mine = mine;
        }

        public override void Step()
        {
            if (!ValidMiningState())
            {
                // Delete job
                _context.WorkerContext().ClearJob();
            }

            if (_progress >= _mineTime)
            {
                // Spawn ore if was ore tile and mining
                if (_mine && _context.gameManager.MapManager.Map[_tile.X, _tile.Y] == Tile.Ore)
                    _context.gameManager.PlayerFactions[0].AddEntity(Constants.Entities.Ore.ID, _tile);

                // Update game server map representation
                _context.gameManager.MapManager.Map[_tile.X, _tile.Y] = 0;

                // Trigger triggerables
                _context.gameManager.MapManager.TileTriggerGrid.TriggerAt(_tile, _context.factionID);

                // Delete job
                _context.WorkerContext().ClearJob();
            }

            // Step mine progress
            _progress += _context.gameManager.DeltaTime;
        }

        private bool ValidMiningState()
        {
            if (ByteVector2.ManhattanDistance(_context.OwnerContext().GetPosition(), _tile) <= 2  // Miner is "close enough"
                && _context.gameManager.MapManager.Map[_tile.X, _tile.Y] > 0)                       // Tile is still solid
                return true;

            Debug.Log("Faction Manager: Invalid mining job state detected, deleting job");
            return false;
        }
    }


    public class FuseJob : Job
    {
        public enum Shape
        {
            Square,
            Circle,
            Diamond
        }

        private double _fuseTime = 0;
        private double _goalTime;
        private int _damage;
        private int _radius;
        private Shape _shape;

        public FuseJob(float time, int damage, int radius, Shape shape, Context context) : base(context)
        {
            _goalTime = time;
            _damage = damage;
            _radius = radius;
            _shape = shape;
        }

        public override void Step()
        {
            _fuseTime += _context.gameManager.DeltaTime;

            if (_fuseTime >= _goalTime)
                try { // TODO: whyyyy does this fail sometimes????
                    Boom();
                }
                catch(System.Exception e)
                {
                    Debug.Log("SERVER EXCEPTION: BOOM FAILED!!! "+ e);
                }
        }

        private void Boom()
        {

            ByteVector2 location = _context.OwnerContext().GetPosition();

            // MUST DIE FIRST lol
            //_context.WorkerContext().x(); // TODO: is necessary????
            _context.OwnerContext().Die();

            _context.gameManager.DamageAllFactionEntities(location, _radius, _damage);

            switch (_shape)
            {
                case Shape.Circle:
                    _context.MapContext().PunchCircle(_radius - 1, location);
                    break;

                case Shape.Square:
                    _context.MapContext().PunchSquare(_radius - 1, location);
                    break;

                case Shape.Diamond:
                    _context.MapContext().PunchDiamond(_radius - 1, location);
                    break;

            }
        }
    }

    public class ReconJob : Job
    {

        private double _curTime = 0;
        private double _goalTime;
        private int _radius;

        public ReconJob(float time, int radius, Context context) : base(context)
        {
            _goalTime = time;
            _radius = radius;
        }

        public override void Step()
        {
            _curTime += _context.gameManager.DeltaTime;
            if (_curTime >= _goalTime)
                try
                { // TODO: whyyyy does this fail sometimes????
                    Reveal();
                }
                catch (System.Exception e)
                {
                    Debug.Log("SERVER EXCEPTION: REVEAL FAILED!!! " + e);
                }
        }

        private void Reveal()
        {

            ByteVector2 location = _context.OwnerContext().GetPosition();

            _context.FactionContext().RevealTiles(_context.MapContext().GetTilesInDiamond(location, 15));

            _context.WorkerContext().ClearJob(); // TODO: is necessary????
            _context.OwnerContext().Die();
        }
    }

    public class SandJob : Job
    {
        private double _fuseTime = 0;
        private double _goalTime;
        private int _radius;

        public SandJob(float time, int radius, Context context) : base(context)
        {
            _goalTime = time;
            _radius = radius;

            Debug.Log($"NEW SAND JOB {_context.factionID} {_context.ownerID}");
        }

        public override void Step()
        {
            Debug.Log("FUSE STEP!");
            _fuseTime += _context.gameManager.DeltaTime;
            if (_fuseTime >= _goalTime)
                try
                { // TODO: whyyyy does this fail sometimes????
                    Bam();
                }
                catch (System.Exception e)
                {
                    Debug.Log("SERVER EXCEPTION: BAM FAILED!!! " + e);
                }

        }

        private void Bam()
        {

            ByteVector2 location = _context.OwnerContext().GetPosition();

            // MUST DIE FIRST lol
            _context.OwnerContext().Die();

            _context.MapContext().FillSquare(_radius - 1, location);

        }
    }

    public class HealJob : Job
    {
        private double _curTime = 0;
        private double _goalTime;
        private int _radius;
        private int _amount;

        public HealJob(float interval, int amount, int radius, Context context) : base(context)
        {
            _goalTime = interval;
            _radius = radius;
            _amount = amount;
        }

        public override void Step()
        {
            _curTime += _context.gameManager.DeltaTime;
            if (_curTime >= _goalTime)
                try
                { // TODO: whyyyy does this fail sometimes????
                    Heal();
                    _curTime -= _goalTime;
                }
                catch (System.Exception e)
                {
                    Debug.Log("SERVER EXCEPTION: HEAL FAILED!!! " + e);
                }

        }

        private void Heal()
        {
            foreach (Entity e in _context.MapContext().GetEntities(_context.OwnerContext().GetPosition(), _radius))
            {
                e.Heal(_amount);
            }
        }
    }

    /////////////////////////////////////////////////////////////
    ////                                                     ////
    ////                   3. Behaviors                      ////
    ////                                                     ////
    /////////////////////////////////////////////////////////////

    public interface IWorker
    {
        void Step();
        void ClearJob();
    }

    public interface IMover
    {
        bool Move(ByteVector2 there);
        double GetSpeed();
    }

    public interface IAttacker
    {
        void ResetAttack();
        void AutoAttack();
    }

    public interface ICarrier
    {
        bool Carry(Entity that);
        bool Pass(Entity toThat, int inventoryID = 0);
        bool Drop(int index = 0);
        bool TryDeploy(int index = 0);
    }

    public interface IHasInventory
    {
        bool Recieve(Entity that);
    }

    public interface ICarriable
    {
        bool Pickup();
        bool Place(ByteVector2 there);
    }

    public interface IDeployable : ICarriable
    {
        bool Deploy(ByteVector2 there, byte inFaction);
    }

    public interface IDigger
    {
        bool Dig(ByteVector2 that);
    }

    public interface ITriggerable
    {
        void Trigger();
    }

    public interface ICripplable
    {
        void Cripple();
    }

    public interface IGear
    {
        // Equipment IDEAS:
        // - Sniper: range%++, attk_cd+ (bad)   ---> For long range harassment, less ideal in close quarters
        // - Big Armor: health++, move_speed-   ---> Two big armors outsurvives hollowpoint + overclock but can't pursue
        // - Nitro: move_speed+
        // - Hollowpoint: attack+               ---> Combines with overclock for biggest damage, 
        // - Overclock: attk_cd--, health-      ---> For hit & run tactics, need heals
        // - Grindstone: dig_speed+
        // - Exp-Card: very expensive, +2 slots, can't stack
        // - Gas Mask: gas immunity, if gas is implemented
        // - Explosive Bullets: AoE Damage, very expensive
    }

    ///////////////////////////////////////////////////////////////
    ////                                                       ////
    ////                   4. Entities                         ////
    ////                                                       ////
    ///////////////////////////////////////////////////////////////



    // An agent that represents a non-tile object in the game state
    // Entities may be completely inert
    // Or they may recieve commands and execute jobs
    public abstract class Entity
    {
        // Game context
        protected Context _context;

        // All entities have a type that defines appearance
        public byte Type { get; protected set; }

        // All entities exist at a position
        protected ByteVector2 CurrentPosition;

        protected byte VisionDistance;

        // All entites have health
        protected int HealthPoints = 1;
        protected int HealthCapacity = 1;

        protected bool Hidden = false;     // In the world?
        protected bool Cloaked = false;
        protected bool Dead = false;       // About to be destroyed?

        public Entity(ByteVector2 at, Context context)
        {
            CurrentPosition = at;
            _context = context;
            _context.gameManager.MapManager.TileOccupationGrid.Add(this, CurrentPosition);
        }

        #region Default Entity API 

        public virtual void Hide()
        {
            Hidden = true;
            _context.gameManager.MapManager.TileOccupationGrid.Remove(this, CurrentPosition);
        }

        public ByteVector2[] GetTerritory()
        {
            return _context.MapContext().GetTerritory(CurrentPosition, VisionDistance);
        }

        public ByteVector2 GetPosition()
        {
            return CurrentPosition;
        }

        public virtual void SetPosition(ByteVector2 here)
        {
            // Display in world
            Hidden = false;

            // Update state on tile occupation grid
            _context.gameManager.MapManager.TileOccupationGrid.Move(this, CurrentPosition, here);

            // Update unit state
            CurrentPosition = here;

            // Trigger triggerables
            _context.gameManager.MapManager.TileTriggerGrid.TriggerAt(here, _context.factionID);
        }

        public virtual bool Heal(int amount)
        {
            if (Dead)
                return false;

            HealthPoints += amount;
            if (HealthPoints > HealthCapacity)
                HealthPoints = HealthCapacity;

            return true;
        }

        public virtual bool Damage(int amount)
        {
            if (Dead)
                return false;

            if (HealthPoints < amount)
                HealthPoints = 0;
            else
                HealthPoints -= amount;

            if (HealthPoints == 0)
                Die();

            return true;
        }

        public virtual void Die()
        {
            Debug.Log("ENTITY DIED: "+Type);

            Dead = true;
            BroadcastState();

            _context.gameManager.MapManager.TileOccupationGrid.Remove(this, CurrentPosition);
            _context.gameManager.PlayerFactions[_context.factionID].Entities[_context.ownerID] = null;
        }

        public bool InFaction(int factionID)
        {
            return factionID == _context.factionID;
        }

        public bool InRange(Entity that, int range)
        {
            if (this == that)
            {
                Debug.Log("Server Faction Manager: You're always near yourself dummy!");
                return true;
            }

            if (that == null)
            {
                Debug.Log("Server Faction Manager: ERROR can't be near null entity.");
                return false;
            }

            return ByteVector2.ManhattanDistance(CurrentPosition, that.CurrentPosition) <= range;
        }

        #endregion

        public virtual void SendState(NewPackets.EntityUpdate statePacket = default)
        {
            statePacket.unitIndex = _context.ownerID;
            statePacket.unitType = Type;
            statePacket.hidden = Hidden;
            statePacket.dead = Dead;

            statePacket.position = CurrentPosition;

            statePacket.health = HealthPoints;
            statePacket.healthCapacity = HealthCapacity;

            for(int i = 1; i < _context.gameManager.PlayerFactions.Count; i++)
            {
                if (_context.gameManager.PlayerFactions[i].Territory.Contains(CurrentPosition))
                {
                    if(!Cloaked || InFaction(i) || InFaction(0))    // Send only if not cloaked or i is own faction
                        _context.gameManager.Send(
                            (byte)_context.factionID,
                            NewPackets.PacketType.UpdateEntity,
                            statePacket.ByteArray(),
                            _context.gameManager.PlayerFactions[i].FactionClient);
                }
            }
        }

        public virtual void BroadcastState(NewPackets.EntityUpdate statePacket = default)
        {
            statePacket.unitIndex = _context.ownerID;
            statePacket.unitType = Type;
            statePacket.hidden = Hidden;
            statePacket.dead = Dead;

            statePacket.position = CurrentPosition;

            statePacket.health = HealthPoints;
            statePacket.healthCapacity = HealthCapacity;

            _context.gameManager.Broadcast(
                    (byte)_context.factionID,
                    NewPackets.PacketType.UpdateEntity,
                    statePacket.ByteArray());
        }
    }

    public abstract class Worker : Entity, IWorker
    {
        protected Job _job;
        protected Constants.Entities.Status _status;

        public Worker(ByteVector2 at, Context context) : base(at, context)
        { }
        
        
        
        public virtual void Step()
        {
            if (_job != null)
            {
                _job.Step();
            }
            else
            {
                _status = Constants.Entities.Status.Idle;
            }
        }

        public virtual void ClearJob()
        {
            _job = null;
        }

        public override void Die()
        {
            base.Die();
        }

        public override void SendState(NewPackets.EntityUpdate statePacket = default)
        {
            statePacket.status = _status;

            base.SendState(statePacket);
        }

        public override void BroadcastState(NewPackets.EntityUpdate statePacket = default)
        {
            statePacket.status = _status;

            base.BroadcastState(statePacket);
        }
    }

    public abstract class Unit : Worker, IMover, IHasInventory, ICarrier, IAttacker, ICripplable
    {
        // Movement
        protected float _moveSpeed = 3;
        protected float _currentSpeed = 3;
        protected float _acceleration = 1;

        // Inventory
        protected byte _inventorySize = 1;
        protected List<Entity> _inventory = new List<Entity>();

        // Combat
        protected Entity _attackTarget = null;
        protected int _attackDamage = 1;
        protected byte _attackRange = 2;
        protected double _attackCooldown = 1;
        protected double _attackSlow = 0;
        protected AttackCooldownJob _currentAttackCooldown = null;

        public Unit(ByteVector2 at, Context context) : base(at, context)
        {

        }

        public override void Step()
        {
            base.Step();

            if (_currentAttackCooldown != null)
                _currentAttackCooldown.Step();
            AutoAttack();

            _currentSpeed += _acceleration * (float)_context.gameManager.DeltaTime;
            if (_currentSpeed > _moveSpeed)
                _currentSpeed = _moveSpeed;
        }

        #region Commandable Unit API 

        // Movement
        public bool Move(ByteVector2 there)
        {
            // Try find path
            ByteVector2[] path = _context.MapContext().GetPath(CurrentPosition, there);

            // Exit if no path found
            if (path == null || path.Length == 0)
                return false;

            // Initialize a new move job
            MoveJob newJob = new MoveJob(path, _context);

            if (_job == null ||                   // If the current job is null, accept
               !_job.Equals(newJob))              // If the current job is not "equivalent," accept
            {
                _job = newJob;
                _status = Constants.Entities.Status.Moving;
                return true;
            }
            return false;
        }

        public double GetSpeed()
        {
            return _currentSpeed;
        }

        // Inventory
        public bool Carry(Entity that)
        {

            // Must be `close` to pickup
            if (!InRange(that, 1))
                return false;

            // Can only carry 'items'
            ICarriable item = that as ICarriable;
            if (that == null)
                return false;

            // Must have space in inventory
            // Item must be 'pickable"
            if (CanCarry() && item.Pickup())
            {
                _inventory.Add(that);
                return true;
            }

            return false;
        }

        // Expand with index
        public bool Pass(Entity toThat, int inventoryID = 0)
        {
            // Must be `close` to pass
            if (!InRange(toThat, 1))
                return false;

            // Can only pass to other carriers
            IHasInventory passee = toThat as IHasInventory;
            if (passee == null)
                return false;

            // Hub case, try "sell" all, always succeeds
            Hub passeeHub = toThat as Hub;
            if (passeeHub != null)
            {
                for (int i = 0; i < _inventory.Count; i++)
                {
                    if (passeeHub.Recieve(_inventory[i]))
                    {
                        _inventory.RemoveAt(i);
                        i--;
                    }
                }
                return true;
            }

            // We must have an item to pass
            if (_inventory.Count > inventoryID && passee.Recieve(_inventory[inventoryID]))
            {
                //BroadcastInteract(); // TODO: REMOVE
                _inventory.RemoveAt(0);
                return true;
            }

            //Status = EntityStatus.Idle; // TODO: REMOVE

            return false;
        }

        public bool Recieve(Entity that)
        {
            // Must have space in inventory
            if (CanCarry())
            {
                _inventory.Add(that);
                return true;
            }

            return false;
        }

        public bool Drop(int index = 0)
        {
            if (_inventory.Count >= index)
                return false;

            ICarriable item = _inventory[index] as ICarriable;
            if (item == null)
            {
                Debug.Log("ENTITY: ERROR, NON-ITEM IN INVENTORY");
                return false;
            }

            if (item.Place(CurrentPosition))
            {
                _inventory.RemoveAt(index);
                return true;
            }

            return false;
        }

        public bool TryDeploy(int index = 0)
        {

            if (_inventory.Count <= index)
            {
                Debug.Log($"ENTITY: ERROR, OUT OF RANGE INVENTORY INDEX: {index} {_inventory.Count}");
                return false;
            }
            IDeployable item = _inventory[index] as IDeployable;
            if (item == null)
            {
                Debug.Log("ENTITY: ERROR, NON-ITEM IN INVENTORY");
                return false;
            }

            if (item.Deploy(CurrentPosition, (byte)_context.factionID))
            {
                _inventory.RemoveAt(index);
                return true;
            }

            return false;
        }

        private bool CanCarry()
        {
            if (_inventory.Count < _inventorySize)
            {
                return true;
            }
            return false;
        }

        public void ResetAttack()
        {
            _currentAttackCooldown = null;
        }

        // TODO: optimize this somehow
        public void AutoAttack()
        {
            if (_attackTarget != null)
            {
                if (!InRange(_attackTarget, _attackRange) || !_context.MapContext().CheckLOS(CurrentPosition, _attackTarget.GetPosition()))
                {
                    _attackTarget = null;
                }
                else if (_currentAttackCooldown == null)
                {
                    ByteVector2 attackSpot = _attackTarget.GetPosition();
                    Entity[] toAttack = _context.MapContext().GetEntities(attackSpot, 0);

                    // Damage nearby
                    foreach (Entity e in toAttack)
                    {
                        if (!e.InFaction(_context.factionID) && e != _attackTarget)
                            Attack(e);
                    }

                    if (Attack(_attackTarget))
                        _currentAttackCooldown = new AttackCooldownJob(_attackCooldown, _context);
                    else
                        _attackTarget = null;
                }
            }
            else
            {
                // Try to find a target
                Entity[] surroundings = _context.gameManager.MapManager.GetEntities(CurrentPosition, _attackRange);
                foreach (Entity e in surroundings)
                {
                    // Only target hostile faction entities
                    if (!e.InFaction(_context.factionID) && !e.InFaction(0) 
                    && ((byte)e.Type <= Constants.Entities.UNIT_IDS || e.Type == Constants.Entities.Hub.ID || e.Type == Constants.Entities.SandBags.ID))
                    {
                        _attackTarget = e;
                        break;
                    }
                }
            }
        }

        private bool Attack(Entity e)
        {
            // Try to cripple
            ICripplable crip = e as ICripplable;
            if (crip != null)
                crip.Cripple();

            return e.Damage(_attackDamage);
        }

        public override bool Damage(int amount)
        {
            return base.Damage(amount);
        }

        public void Cripple()
        {
            _currentSpeed = 1f;
        }

        #endregion

        public override void SendState(NewPackets.EntityUpdate statePacket = default)
        {
            statePacket.moveSpeed = _currentSpeed;

            statePacket.carryCapacity = _inventorySize;

            List<byte> invRep = new List<byte>();
            foreach (Entity item in _inventory)
            {
                invRep.Add(item.Type);
            }
            statePacket.inventory = invRep;

            if (_attackTarget != null)
            {
                statePacket.attacking = true;
                statePacket.attackTarget = _attackTarget.GetPosition();
            }
            statePacket.attackDamage = _attackDamage;
            statePacket.attackRange = _attackRange;

            base.SendState(statePacket);
        }

        public override void BroadcastState(NewPackets.EntityUpdate statePacket = default)
        {
            statePacket.moveSpeed = _moveSpeed;

            statePacket.carryCapacity = _inventorySize;

            List<byte> invRep = new List<byte>();
            foreach (Entity item in _inventory)
            {
                invRep.Add(item.Type);
            }
            statePacket.inventory = invRep;

            if (_attackTarget != null)
            {
                statePacket.attacking = true;
            }
            statePacket.attackDamage = _attackDamage;
            statePacket.attackRange = _attackRange;

            base.BroadcastState(statePacket);
        }
    }

    public abstract class Item : Entity, ICarriable
    {
        public Item(ByteVector2 at, Context context) : base(at, context)
        { }

        public bool Pickup()
        {
            if (!Hidden)
            {
                Hide();
                return true;
            }
            return false;
        }

        public bool Place(ByteVector2 there)
        {
            if (Hidden)
            {
                SetPosition(there);
                return true;
            }
            return false;
        }
    }

    // ew lol
    public abstract class WorkerItem : Worker, ICarriable
    {
        public WorkerItem(ByteVector2 at, Context context) : base(at, context)
        { }

        public bool Pickup()
        {
            if (!Hidden)
            {
                Hide();
                return true;
            }
            return false;
        }

        public bool Place(ByteVector2 there)
        {
            if (Hidden)
            {
                SetPosition(there);
                return true;
            }
            return false;
        }
    }

    public abstract class Deployable : WorkerItem, IDeployable
    {

        public Deployable(ByteVector2 at, Context context) : base(at, context)
        { }

        public virtual bool Deploy(ByteVector2 there, byte inFaction)
        {
            // Discard potentially stale jobs
            _job = null;

            // Leave old faction (should be neutral)
            _context.FactionContext().Entities[_context.ownerID] = null;

            // Join new faction (should be player faction)
            _context.factionID = inFaction;
            _context.ownerID = _context.FactionContext().GetFreeEntityIndex();
            _context.FactionContext().Entities[_context.ownerID] = this;

            // Move and unhide
            SetPosition(there);
            // Display in world
            // Hidden = false;

            // // Update state on tile occupation grid
            // _context.gameManager.MapManager.TileOccupationGrid.Add(this, there);
            // CurrentPosition = there;

            _status = Constants.Entities.Status.Deployed;

            return true;
        }
    }

    public abstract class Triggerable : Deployable
    {

        public Triggerable(ByteVector2 at, Context context) : base(at, context)
        { }

        public override bool Deploy(ByteVector2 there, byte inFaction)
        {
            base.Deploy(there, inFaction);
            _context.gameManager.MapManager.TileTriggerGrid.ListenAt(this, CurrentPosition);

            return true;
        }

        public virtual void Trigger()
        {
            _context.gameManager.MapManager.TileTriggerGrid.UnlistenAt(this, CurrentPosition);
        }
    }

    // "REAL" Entities

    public class Hub : Entity, IHasInventory
    {
        public Hub(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.Hub.ID;
            VisionDistance = Constants.Entities.Hub.VISION;
            HealthCapacity = Constants.Entities.Hub.HEALTH;
            HealthPoints = HealthCapacity;
        }

        public bool Recieve(Entity that)
        {
            Ore toSell = that as Ore;
            if (toSell == null)
                return false;

            toSell.Die();
            _context.FactionContext().Credits += Constants.Global.ORE_SELL_VALUE;

            return true;
        }
    }

    public class Ore : Item
    {
        public Ore(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.Ore.ID;
            HealthCapacity = Constants.Entities.Hub.HEALTH;
            HealthPoints = HealthCapacity;
        }
    }

    // "REAL" Units

    public class Miner : Unit, IDigger
    {
        public Miner(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.Miner.ID;
            VisionDistance = Constants.Entities.Miner.VISION;
            HealthCapacity = Constants.Entities.Miner.HEALTH;
            HealthPoints = HealthCapacity;
            _moveSpeed = Constants.Entities.Miner.SPEED;
            _inventorySize = Constants.Entities.Miner.INVENTORY;

            _attackDamage = Constants.Entities.Miner.ATTACK_DAMAGE;
            _attackCooldown = Constants.Entities.Miner.ATTACK_INTERVAL;
            _attackRange = Constants.Entities.Miner.ATTACK_RANGE;
        }

        public bool Dig(ByteVector2 that)
        {
            if (_context.gameManager.MapManager.Map[that.X, that.Y] == 0) // If the tile at index isn't solid
                return false;

            _job = new DigJob(that, (float)_attackCooldown, true, _context);

            _status = Constants.Entities.Status.Working;

            return true;
        }
    }

    public class Digger : Unit, IDigger
    {
        public Digger(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.Digger.ID;
            VisionDistance = Constants.Entities.Digger.VISION;
            HealthCapacity = Constants.Entities.Digger.HEALTH;
            HealthPoints = HealthCapacity;
            _moveSpeed = Constants.Entities.Digger.SPEED;
            _inventorySize = Constants.Entities.Digger.INVENTORY;

            _attackDamage = Constants.Entities.Digger.ATTACK_DAMAGE;
            _attackCooldown = Constants.Entities.Digger.ATTACK_INTERVAL;
            _attackRange = Constants.Entities.Digger.ATTACK_RANGE;
        }

        public bool Dig(ByteVector2 that)
        {
            if (_context.gameManager.MapManager.Map[that.X, that.Y] == 0) // If the tile at index isn't solid
                return false;

            _job = new DigJob(that, (float)_attackCooldown, false, _context);

            _status = Constants.Entities.Status.Working;

            return true;
        }
    }

    public class Soldier : Unit
    {
        public Soldier(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.Soldier.ID;
            VisionDistance = Constants.Entities.Soldier.VISION;
            HealthCapacity = Constants.Entities.Soldier.HEALTH;
            HealthPoints = HealthCapacity;
            _moveSpeed = Constants.Entities.Soldier.SPEED;
            _inventorySize = Constants.Entities.Soldier.INVENTORY;

            _attackDamage = Constants.Entities.Soldier.ATTACK_DAMAGE;
            _attackCooldown = Constants.Entities.Soldier.ATTACK_INTERVAL;
            _attackRange = Constants.Entities.Soldier.ATTACK_RANGE;
        }
    }

    public class Hauler : Unit
    {
        public Hauler(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.Hauler.ID;
            VisionDistance = Constants.Entities.Hauler.VISION;
            HealthCapacity = Constants.Entities.Hauler.HEALTH;
            HealthPoints = HealthCapacity;
            _moveSpeed = Constants.Entities.Hauler.SPEED;
            _inventorySize = Constants.Entities.Hauler.INVENTORY;

            _attackDamage = Constants.Entities.Hauler.ATTACK_DAMAGE;
            _attackCooldown = Constants.Entities.Hauler.ATTACK_INTERVAL;
            _attackRange = Constants.Entities.Hauler.ATTACK_RANGE;
        }
    }

    // "REAL" Deployables

    public class ProximityMine : Triggerable
    {
        protected int _attackDamage;
        protected float _fuseTime;
        protected byte _explosionRadius;

        public ProximityMine(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.ProximityMine.ID;
            VisionDistance = Constants.Entities.ProximityMine.VISION;
            Cloaked = Constants.Entities.ProximityMine.CLOAKED;
            HealthCapacity = Constants.Entities.ProximityMine.HEALTH;
            HealthPoints = HealthCapacity;

            _attackDamage = Constants.Entities.ProximityMine.ATTACK_DAMAGE;
            _fuseTime = Constants.Entities.ProximityMine.FUSE_TIME;
            _explosionRadius = Constants.Entities.ProximityMine.ATTACK_RANGE;
        }

        public override bool Damage(int amount)
        {
            float delayModifier = (float)(_context.gameManager.NumberGenerator.NextDouble() * _fuseTime);

            if (_job == null)
                _job = new FuseJob((float)_fuseTime - delayModifier, _attackDamage, _explosionRadius, FuseJob.Shape.Diamond, _context);

            return true;
        }

        public override void Trigger()
        {
            base.Trigger();

            if (_job == null)
                _job = new FuseJob((float)_fuseTime, _attackDamage, _explosionRadius, FuseJob.Shape.Diamond, _context);
        }
    }

    public class TimeBomb : Deployable
    {
        protected int _attackDamage;
        protected float _fuseTime;
        protected int _explosionRadius;

        public TimeBomb(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.TimeBomb.ID;
            VisionDistance = Constants.Entities.TimeBomb.VISION;
            HealthCapacity = Constants.Entities.TimeBomb.HEALTH;
            HealthPoints = HealthCapacity;

            _attackDamage = Constants.Entities.TimeBomb.ATTACK_DAMAGE;
            _fuseTime = Constants.Entities.TimeBomb.FUSE_TIME;
            _explosionRadius = Constants.Entities.TimeBomb.ATTACK_RANGE;
        }

        public override bool Damage(int amount)
        {
            float delayModifier = (float)(_context.gameManager.NumberGenerator.NextDouble() * _fuseTime);

            if (_job == null)
                _job = new FuseJob((float)_fuseTime - delayModifier, _attackDamage, _explosionRadius, FuseJob.Shape.Circle, _context);

            return true;
        }

        public override bool Deploy(ByteVector2 there, byte inFaction)
        {
            if (!base.Deploy(there, inFaction))
                return false;

            if (_job == null)
                _job = new FuseJob((float)_fuseTime, _attackDamage, _explosionRadius, FuseJob.Shape.Circle, _context);

            return true;
        }
    }

    public class AntimatterBomb : Deployable
    {
        protected int _attackDamage;
        protected float _fuseTime;
        protected int _explosionRadius;

        public AntimatterBomb(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.AntimatterBomb.ID;
            VisionDistance = Constants.Entities.AntimatterBomb.VISION;
            HealthCapacity = Constants.Entities.AntimatterBomb.HEALTH;
            HealthPoints = HealthCapacity;

            _attackDamage = Constants.Entities.AntimatterBomb.ATTACK_DAMAGE;
            _fuseTime = Constants.Entities.AntimatterBomb.FUSE_TIME;
            _explosionRadius = Constants.Entities.AntimatterBomb.ATTACK_RANGE;
        }

        public override bool Damage(int amount)
        {
            float delayModifier = (float)(_context.gameManager.NumberGenerator.NextDouble() * _fuseTime);

            if (_job == null)
                _job = new FuseJob((float)_fuseTime - delayModifier, _attackDamage, _explosionRadius, FuseJob.Shape.Square, _context);

            return true;
        }

        public override bool Deploy(ByteVector2 there, byte inFaction)
        {
            if (!base.Deploy(there, inFaction))
                return false;

            if (_job == null)
                _job = new FuseJob((float)_fuseTime, _attackDamage, _explosionRadius, FuseJob.Shape.Square, _context);

            return true;
        }

        public override void SendState(NewPackets.EntityUpdate statePacket = default(NewPackets.EntityUpdate))
        {
            base.BroadcastState(statePacket);
        }
    }

    public class QuickSand : Deployable
    {
        protected float _fuseTime;
        protected int _explosionRadius;

        public QuickSand(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.QuickSand.ID;
            VisionDistance = Constants.Entities.QuickSand.VISION;
            HealthCapacity = Constants.Entities.QuickSand.HEALTH;
            HealthPoints = HealthCapacity;

            _fuseTime = Constants.Entities.QuickSand.FUSE_TIME;
            _explosionRadius = Constants.Entities.QuickSand.ATTACK_RANGE;
        }

        public override bool Deploy(ByteVector2 there, byte inFaction)
        {
            if (!base.Deploy(there, inFaction))
                return false;

            if (_job == null)
                _job = new SandJob((float)_fuseTime, _explosionRadius, _context);

            return true;
        }
    }

    public class GasBomb : Deployable
    {
        public GasBomb(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.GasBomb.ID;
            VisionDistance = Constants.Entities.GasBomb.VISION;
            HealthCapacity = Constants.Entities.GasBomb.HEALTH;
            HealthPoints = HealthCapacity;
        }

        public override bool Deploy(ByteVector2 there, byte inFaction)
        {
            if (!base.Deploy(there, inFaction))
                return false;

            _context.MapContext().TileGasManager.RegisterEmitter(this);

            return true;
        }

        public override void Die()
        {
            _context.MapContext().TileGasManager.DeregisterEmitter(this);
            base.Die();
        }
    }

    public class SandBags : Deployable
    {
        public SandBags(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.SandBags.ID;
            VisionDistance = Constants.Entities.SandBags.VISION;
            HealthCapacity = Constants.Entities.SandBags.HEALTH;
            HealthPoints = HealthCapacity;
        }
    }

    public class HealStation : Deployable
    {

        private float _interval;
        private int _amount;
        private int _radius;


        public HealStation(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.HealStation.ID;
            VisionDistance = Constants.Entities.HealStation.VISION;
            HealthCapacity = Constants.Entities.HealStation.HEALTH;
            HealthPoints = HealthCapacity;

            _interval = Constants.Entities.HealStation.HEAL_INTERVAL;
            _amount = Constants.Entities.HealStation.HEAL_AMOUNT;
            _radius = Constants.Entities.HealStation.HEAL_RADIUS;
        }

        public override bool Deploy(ByteVector2 there, byte inFaction)
        {
            if (!base.Deploy(there, inFaction))
                return false;

            _job = new HealJob(_interval, _amount, _radius, _context);

            return true;
        }
    }

    public class SonicBomb : Deployable
    {

        private float _chargeTime;
        private int _radius;

        public SonicBomb(ByteVector2 at, Context context) : base(at, context)
        {
            Type = Constants.Entities.SonicBomb.ID;
            VisionDistance = Constants.Entities.SonicBomb.VISION;
            HealthCapacity = Constants.Entities.SonicBomb.HEALTH;
            HealthPoints = HealthCapacity;

            _chargeTime = Constants.Entities.SonicBomb.CHARGE_TIME;
            _radius = Constants.Entities.SonicBomb.REVEAL_RADIUS;
        }

        public override bool Deploy(ByteVector2 there, byte inFaction)
        {
            if (!base.Deploy(there, inFaction))
                return false;

            _job = new ReconJob(_chargeTime, _radius, _context);

            return true;
        }
    }
}
