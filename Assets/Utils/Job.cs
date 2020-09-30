using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TeamBlack.MoonShot.Networking;
using TeamBlack.Util;
using UnityEngine;
using Job = TeamBlack.Util.Job;

namespace TeamBlack.Util
{
    public enum JobStatus : byte { Null, Beginning, Running, Done } 
    
    public interface IJob
    { 
        JobStatus Status { get; }

        void OnBegin(); // 
        void Update();  // every time a job needs to update
        void OnDone();  // when the job is completed
    }
    
    public class Job : IJob
    {
        protected JobStatus _status = JobStatus.Null;
        public JobStatus Status => _status;

        public Queue<IJob> ToDo = new Queue<IJob>();


        public virtual void OnBegin()
        {
            _status = JobStatus.Beginning;
        }

        private IJob _curr;
        public virtual void Update()
        {
            if (Status == JobStatus.Null)
            {
                _status = JobStatus.Beginning;
                OnBegin();
            }
            else if (Status == JobStatus.Beginning) _status = JobStatus.Running;

            if (ToDo.Count == 0)
            {
                _status = JobStatus.Done;
                OnDone();
                return;
            }
            else if (_curr == null || _curr.Status == JobStatus.Done)
                _curr = ToDo.Dequeue();
            
            _curr.Update();
        }

        public virtual void OnDone()
        {
        }
    }

    public class ActionJob : Job
    {
        private Action _action;
        public ActionJob(Action action)
        {
            _action = action;
        }

        public override void Update()
        {
            //Debug.Log("running action");
            _action();
            _status = JobStatus.Done;
        }
    }
}

namespace TeamBlack.MoonShot
{
    public class MoonShotJob : Job
    {
        public HashSet<Vector2Int> JobTiles;
        public Entity Entity { private set; get; }

        public MoonShotJob(Entity owner)
        {
            Entity = owner;
        }
    }

    public class MoveJob : MoonShotJob
    {
        public MoveJob(Entity owner, Vector2Int pos) : base(owner)
        {
            // build packet
            ToDo.Enqueue(
                new ActionJob(
                    () =>
                    {
                        NewPackets.TileInteraction tilePacket;
                        tilePacket.tileX = (byte)pos.x;
                        tilePacket.tileY = (byte)pos.y;
                        tilePacket.entityIndex = owner.entityID;

                        NeoPlayer.Instance.Client.Send(tilePacket.ByteArray(), NewPackets.PacketType.TileInteractRequest, (byte)NeoPlayer.Instance.myFactionID);
                    }
                )    
            );
        }

        public MoveJob(Entity owner, Vector2 pos) : this(owner, NeoPlayer.Instance.MapManager.GridIndexFromWorldPos(pos))
        {
        }
    }
    
    public class InteractJob : MoonShotJob
    {
        public InteractJob(Entity owner, int interactID, int interactFactionID) : base(owner)
        {
            var np = NeoPlayer.Instance;
            byte currId = (byte)owner.entityID;
            // build packet
            ToDo.Enqueue(
                new ActionJob(
                    () =>
                    {
                        NewPackets.EntityInteraction entityInteractionPacket;
                        entityInteractionPacket.myEntityIndex = currId;
                        entityInteractionPacket.otherFactionIndex = (byte)interactFactionID; // TODO: remove byte when interaface changes
                        entityInteractionPacket.otherEntityIndex = (byte)interactID;

                        np.Client.Send(entityInteractionPacket.ByteArray(), NewPackets.PacketType.EntityInteractRequest, (byte)np.myFactionID);
                    }
                )    
            );
        }
        public InteractJob(Entity owner, Entity interact) : this(owner, interact.entityID, interact.factionID)
        {
        }
    }

    public class MoveInteractJob : MoonShotJob
    {
        public MoveInteractJob(Entity owner, Entity interact) : base(owner)
        {
            var np = NeoPlayer.Instance;
            byte currId = (byte)interact.entityID;

            if (owner == null)
            {
                Debug.LogWarning("MoveInteraction jobs owner has died");
                return;
            }

            if (Vector2.Distance(
                    interact.transform.position,
                    owner.transform.position) >= owner.AttackRange)
            {
                ToDo.Enqueue(new MoveJob(owner, interact.transform.position));
            }
            ToDo.Enqueue(new InteractJob(owner, interact));
        }
    }

    public class MineJob : MoonShotJob
    {
        public MineJob(Entity owner, HashSet<Vector2Int> toMine) : base(owner)
        {
            JobTiles = toMine;
            if (toMine.Count != 0) ToDo.Enqueue(new ActionJob(()=>MineAreaTask(toMine)));
        }

        private ByteVector2[] GetPath(Vector2Int block, Vector2Int movePos)
        {
            var np = NeoPlayer.Instance;
            ByteVector2[] path;

            if (Entity.Type == Constants.Entities.Digger.ID && np.MapManager[block] != Tile.Stone) path = null;
            else if (Entity.Type == Constants.Entities.Miner.ID  && np.MapManager[block] != Tile.Ore) path = null;
            else path = np.PathFinder.FindBestPath((ByteVector2)Entity.tilePos, (ByteVector2)movePos);
            return path;
        }
        private void MineAreaTask(HashSet<Vector2Int> toMine)
        {
            var np = NeoPlayer.Instance;

            if (toMine.Count == 0)
                return;

            // TODO: priority q
            var potentialPaths = new List<Tuple<Vector2Int, Vector2Int>>();

            var toRemove = new List<Vector2Int>();
            foreach (var pos in toMine)
            {
                if (np.MapManager[pos] == Tile.Fog) continue;
                if (np.MapManager[pos] == Tile.Empty)
                {
                    toRemove.Add(pos);
                    continue;
                }
                var searchPos = new Vector2Int[4]
                {
                    pos + Vector2Int.right,
                    pos + Vector2Int.up,
                    pos + Vector2Int.left,
                    pos + Vector2Int.down
                 };
                
                foreach (var search in searchPos)
                {
                    if (search.x < np.MapManager.MapWidth && np.MapManager[search] == Tile.Empty
                        && search.x >= 0 && np.MapManager[search] == Tile.Empty
                        && search.y < np.MapManager.MapHeight && np.MapManager[search] == Tile.Empty
                        && search.y >= 0 && np.MapManager[search] == Tile.Empty
                        )
                    {
                        potentialPaths.Add(new Tuple<Vector2Int, Vector2Int>(search, pos));
                    }
                }
            }

            foreach (var removal in toRemove) toMine.Remove(removal); 
            
            if (potentialPaths.Count == 0) return;
            // Sort by path distance
            potentialPaths.Sort((a, b) => Vector2Int.Distance(a.Item1, Entity.tilePos).CompareTo(Vector2Int.Distance(b.Item1, Entity.tilePos)));
            
            int idx = 0;
            var block = potentialPaths[idx].Item2;
            var movePos = potentialPaths[idx].Item1;
            var path = GetPath(block, movePos);
            while (idx < potentialPaths.Count - 1
                   && (path == null || path.Length == 0))
            {
                var curr = potentialPaths[++idx];
                movePos = curr.Item1;
                block = curr.Item2;

                // if the type is a digger and the tile is not stone, the path is invalid
                path = GetPath(block, movePos);
            }

            if (path == null || path.Length == 0)
                return;

            // goto best tile
            toMine.Remove((Vector2Int)block);

            //FIXME: non idle case?
            
            // set unit stuff
            Unit unit = Entity.GetComponent<Unit>();
            
            
            // TODO: TEST
            ToDo.Enqueue(new ActionJob(() => {
                if (np.MapManager[(Vector2Int)block] == Tile.Empty)
                {
                    ToDo.Dequeue();
                    ToDo.Dequeue();
                    ToDo.Dequeue();
                }}));

            ToDo.Enqueue(new MoveJob(Entity, (Vector2Int)path[path.Length -1]));
            ToDo.Enqueue(new ActionJob(() =>
            {
                if (unit) unit.AnimateMineBlock(block);
            }));
            ToDo.Enqueue(new MoveJob(Entity, block));
            if (toMine.Count != 0) ToDo.Enqueue(new ActionJob(()=>MineAreaTask(toMine)));
        }
    }
    public class CollectionJob : MoonShotJob
        {
            private Entity _owner;
            public CollectionJob(Entity owner, HashSet<Entity> entities) : base(owner)
            {
                _owner = owner;
                ToDo.Enqueue(new ActionJob(()=>CollectionTask(entities)));
            }

            protected void CollectionTask(HashSet<Entity> collectables)
            {
                Debug.Log("collection");

                Entity curr = collectables.GetEnumerator().Current;
                
                ToDo.Enqueue(new MoveJob(_owner, curr.transform.position));
                ToDo.Enqueue(new InteractJob(_owner, curr));
                
                
                collectables.Remove(curr);
                if (collectables.Count > 0) ToDo.Enqueue(new ActionJob(()=>CollectionTask(collectables)));
            }
        }
        
        public class CollectRegionJob : MoonShotJob
        {
            private static HashSet<Dictionary<int, Vector2Int>> _allCollections = new HashSet<Dictionary<int, Vector2Int>>();
        
            protected Vector2Int _startTile;
            protected Vector2Int _endTile;
            protected Dictionary<int, Vector2Int> _toCollect;

            public CollectRegionJob(Entity owner, Vector2Int startTile, Vector2Int endTile) : base(owner)
            {
                _startTile = startTile;
                _endTile = endTile;
                _toCollect = new Dictionary<int, Vector2Int>();
                _allCollections.Add(_toCollect);
                ToDo.Enqueue(new ActionJob(()=> CollectionTask()));
            }
    
            public CollectRegionJob(Entity owner, Vector2Int startTile, Vector2Int endTile, Dictionary<int, Vector2Int> toCollect) : base(owner)
            {
                _startTile = startTile;
                _endTile = endTile;
                _toCollect = toCollect;
                _allCollections.Add(_toCollect);
                ToDo.Enqueue(new ActionJob(()=> CollectionTask()));
            } 
            public static void CollectRegionGroup(IEnumerable<Entity> entities, Vector2Int startTile, Vector2Int endTile)
            {
                var toCollect = new Dictionary<int, Vector2Int>();
                foreach (var e in entities) e.ToDo.Enqueue(new CollectRegionJob(e, startTile, endTile, toCollect));
            }
            
            ~CollectRegionJob()
            {
                _allCollections.Remove(_toCollect);
            }

            protected void ScanRegion()
            {
                var np = NeoPlayer.Instance;
                int count = 0;
                for (int i = 0; i < np.FactionEntities.GetLength(1); i++)
                {
                    
                    var entity = np.FactionEntities[0, i];
                    if (entity != null)
                    {
                        count++;

                        var gridPos = np.MapManager.GridIndexFromWorldPos(entity.transform.position);
                        if (entity.factionID == 0 &&
                            gridPos.x <= Mathf.Max(_startTile.x, _endTile.x) &&
                            gridPos.x >= Mathf.Min(_startTile.x, _endTile.x) &&
                            gridPos.y <= Mathf.Max(_startTile.y, _endTile.y) &&
                            gridPos.y >= Mathf.Min(_startTile.y, _endTile.y) &&
                            !entity.Hidden &&
                            !_toCollect.Keys.Contains(entity.entityID))
                        {
                            _toCollect.Add(entity.entityID, entity.tilePos);
                        }
                    }
                }
                var toRemove = new List<int>();
                foreach (var id in _toCollect.Keys)
                {
                    if (np.FactionEntities[0,id] == null && Vector2Int.Distance(Entity.tilePos, _toCollect[id]) < 1)
                        toRemove.Add(id);
                }

                foreach (var col in _allCollections) foreach (var id in toRemove) col.Remove(id);
            }
            
            protected void CollectionTask()
            {
                var np = NeoPlayer.Instance;
                
                ScanRegion();
                //Debug.Log(String.Join(", ", _toCollect.Values));

                if (_toCollect.Count == 0)
                {
                    if (Entity.Inventory.Contains(Constants.Entities.Ore.ID) && Entity.ToDo.Count == 0)
                    {
                        ToDo.Enqueue(new MoveInteractJob(Entity, np.Hub));
                        ToDo.Enqueue(new ActionJob(CollectionTask));
                    }
                    else if (Entity.ToDo.Count == 0) ToDo.Enqueue(new ActionJob(CollectionTask));
                    return;
                }

                int selected = _toCollect.Keys.OrderBy(
                    id => Vector2Int.Distance(Entity.tilePos, _toCollect[id])).First();
                var val = _toCollect[selected];
                foreach (var col in _allCollections) col.Remove(selected);
               
                ToDo.Enqueue(new MoveJob(Entity, val));
                ToDo.Enqueue(new InteractJob(Entity, selected, 0));
                ToDo.Enqueue(new ActionJob(CollectionTask));
            }
        }
}