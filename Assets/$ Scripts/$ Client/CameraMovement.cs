using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TeamBlack.MoonShot.Networking;
using UnityEngine;
using System.Linq;
using TeamBlack.Util;
using UnityEngine.EventSystems;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TeamBlack.MoonShot
{
    public class CameraMovement : MonoBehaviour
    {
        [SerializeField] private MapManager _myMapManager;
        //[SerializeField] private UnitManager _unitManager;
        [Header("General")]
        [SerializeField] private float _speed = 1;
        [SerializeField] private bool _inverse = true;
        [SerializeField] private float _dragSpeedFactor = 8;
        
        [Header("Edge")] 
        [SerializeField] private bool _edgeMovement = true;
        [SerializeField] [Range(0, .5f)] private float _percentEdgeMove = .2f;
        [SerializeField]  private float _sideScrollSpeedModifier = 10f;

        [Header("Scrolling")]
        [SerializeField] private bool _inverseScroll = true;
        [SerializeField] private int _maxSize = 10;
        [SerializeField] private int _minSize = 1;

        [SerializeField] private GameObject _selectBoxPrefab;
        private GameObject _selectBox;

        private bool _disableSelect = false;
        
        private float _scrollFactor = 3;
        
        private Camera _myCamera;
    
        private Coroutine _smoothRoutine;
        private Coroutine _scrollRoutine;
        
        private Vector2 _prevPosition;
        private float _zVal;

        private float _prevCollect = 0;
        private bool _collectRegion = false;
        private Vector2 _prevTilePos;

        private Vector2 _camPosition
        {
            set
            {
                Vector3 temp = value;
                temp.z = _zVal;
                _myCamera.transform.position = temp;
            }
            get { return _myCamera.transform.position; }
        }
        
        private Vector2 _mousePos => Input.mousePosition; // Screen Space
        
        private void Awake()
        {
            _myCamera = Camera.main;
            _zVal = _myCamera.transform.position.z;
            NeoPlayer.Instance.Selected.Listen(() =>
            {
                _collectRegion = false;
            });
        }

        private Vector2 _startClick = Vector2.zero;

        public void DisableSelectForFrame()
        {
            StartCoroutine(DisableForFrame());
        }

        private IEnumerator DisableForFrame()
        {
            _disableSelect = true;
            yield return new WaitForEndOfFrame();
            _disableSelect = false;
        }

        private void Update()
        {
            NeoPlayer np = NeoPlayer.Instance;

            if (Input.GetKeyDown(KeyCode.Tab) && np.Selected.Value.Count <= 1)
            {
                int currIndex = (np.FrontSelected != null) ? np.FrontSelected.entityID : np.FactionEntities.GetLength(1);

                for (int i = 1; i <= np.FactionEntities.GetLength(1); i++)
                {
                    int idx = (currIndex + i) % np.FactionEntities.GetLength(1);
                    if (np.FactionEntities[np.myFactionID, idx])
                    {
                        np.Selected.Value = new List<Entity>() {np.FactionEntities[np.myFactionID, idx]};
                        break;
                    }
                }
            }
            
            // multi-select
            if (Input.GetMouseButtonDown(0))
            {
                _startClick = _myCamera.ScreenToWorldPoint(Input.mousePosition);
                _selectBox = Instantiate(_selectBoxPrefab);
                _selectBox.transform.position = _startClick;
            }

            if (Input.GetMouseButton(0))
            {
                if (_selectBox)
                {
                    Vector2 clickPos = _myCamera.ScreenToWorldPoint(Input.mousePosition);

                    _selectBox.transform.position = (clickPos + _startClick) / 2;
                    _selectBox.GetComponent<SpriteRenderer>().size =
                        new Vector2(Mathf.Abs(clickPos.x - _startClick.x),Mathf.Abs(clickPos.y - _startClick.y));
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                Destroy(_selectBox);
            }
            if (Input.GetMouseButtonUp(0) && !_disableSelect) 
            {
                Vector2 endClick = _myCamera.ScreenToWorldPoint(Input.mousePosition);
                List<Entity> units = new List<Entity>();
    
                var center = (endClick + _startClick) / 2;
                var selectedBounds = new Bounds(center, new Vector2(Mathf.Abs(_startClick.x - endClick.x), Mathf.Abs(_startClick.y - endClick.y)));
                
                
                
                for (int i= 0; i < np.FactionEntities.GetLength(1); i++)
                {
                    Entity unitEntity = np.FactionEntities[np.myFactionID, i];
                    if (unitEntity)
                    {
                        Unit unit = unitEntity.GetComponent<Unit>();
                        if (unit == null) continue;
                        var unitBounds = unit.GetComponent<SpriteRenderer>().bounds; // TODO: use colider bounds maybe?
                    
                        if (unitBounds.Intersects(selectedBounds) || selectedBounds.Contains(unit.transform.position))
                            units.Add(np.FactionEntities[np.myFactionID, i]);
                    }    
                }
                
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    var copyUnits = new List<Entity>(np.Selected.Value);
                    foreach (var u in units)
                        if (!copyUnits.Contains(u))
                            copyUnits.Add(u);
                    units = copyUnits;
                }
                np.Selected.Value = units;
            }

            // camera movement movement
            float horizontal = 0;
            float vertical = 0;
            
            if (_edgeMovement && !Input.GetMouseButton(0))
            {
                int xBorder = Mathf.RoundToInt(_myCamera.pixelWidth * _percentEdgeMove);
                int yBorder = Mathf.RoundToInt(_myCamera.pixelHeight * _percentEdgeMove);

                if (_mousePos.x < xBorder)
                    horizontal = -_sideScrollSpeedModifier;
                else if (_mousePos.x > _myCamera.pixelWidth - xBorder)
                    horizontal = _sideScrollSpeedModifier;
                if (_mousePos.y < yBorder)
                    vertical = -_sideScrollSpeedModifier;
                else if (_mousePos.y > _myCamera.pixelHeight - yBorder)
                    vertical = _sideScrollSpeedModifier;
            }
            
            if (Input.GetMouseButtonDown(2))
            {
                _prevPosition = _mousePos;
            }
            if (Input.GetMouseButton(2))
            {
                Vector2 currMousePos = _mousePos;
                Vector2 diff = currMousePos - _prevPosition;
                if (_inverse) diff *= -1;

                diff /= new Vector2(_myCamera.pixelWidth, _myCamera.pixelHeight);
                diff *= _dragSpeedFactor;
                
                horizontal = diff.x;
                vertical = diff.y;
                _prevPosition = currMousePos;
            }
            
            horizontal += Input.GetAxis("Horizontal");
            vertical += Input.GetAxis("Vertical");
            
            _camPosition += horizontal * _speed * Time.deltaTime * _scrollFactor * Vector2.right;
            _camPosition += vertical   * _speed * Time.deltaTime * _scrollFactor * Vector2.up;

            if (Input.GetMouseButtonDown(1))
                _prevTilePos = _myCamera.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButton(1))
            {
                Vector2 currPos = _myCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector2Int startTile = np.MapManager.GridIndexFromWorldPos(currPos);
                Vector2Int endTile = np.MapManager.GridIndexFromWorldPos(_prevTilePos);

                for (int i = Mathf.Min(startTile.x, endTile.x); i <= Mathf.Max(startTile.x, endTile.x); i++)
                for (int j = Mathf.Min(startTile.y, endTile.y); j <= Mathf.Max(startTile.y, endTile.y); j++)
                    if (i >= 0 && i < np.MapManager.MapWidth && j >= 0 && j < np.MapManager.MapHeight)
                        np.MapManager.OreMap.SetTileColorFrame((_collectRegion) ? Color.blue : Color.green, new Vector2Int(i,j));

            }
                
            // M2 command
            if (Input.GetMouseButtonUp(1))
            {
                if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    foreach (var u in np.Selected.Value)
                    {
                        u.ToDo.Clear();
                    } 
                }
                GameObject.FindObjectOfType<AudioManager>().PlayTileSelect();

                foreach (var unit in np.Selected.Value) unit.ToDo.Clear();

                Vector2 currPos = _myCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector2Int startTile = np.MapManager.GridIndexFromWorldPos(currPos);
                Vector2Int endTile = np.MapManager.GridIndexFromWorldPos(_prevTilePos);
                
                
                if (_collectRegion)
                {
                    
                    
                    foreach (var unit in np.Selected.Value)
                    {
                        unit.ToDo.Enqueue(new CollectRegionJob(unit, startTile, endTile));
                    }
                    _collectRegion = false;
                }
                else
                {
                    List<Vector2Int> tilesToMine = new List<Vector2Int>();
                    for (int i = Mathf.Min(startTile.x, endTile.x); i <= Mathf.Max(startTile.x, endTile.x); i++) 
                    for (int j = Mathf.Min(startTile.y, endTile.y); j <= Mathf.Max(startTile.y, endTile.y); j++)
                    {
                        if (np.MapManager[i, j] != Tile.Empty)
                        {
                            Vector2Int pos = new Vector2Int(i, j);
                            tilesToMine.Add(pos);
                        }
                    }
                    //_myMapManager.OreMap.Blink(tilesToMine);

                    if (tilesToMine.Count == 0 && np.MapManager[endTile] == Tile.Empty)
                    {
                        //if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))

                        // FIXME: this is garbo please fix

                        // Determined if you clicked on something
                        var worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        var hit = Physics2D.Raycast(worldMousePos, Vector2.zero);
                        if (hit.transform?.GetComponent<Entity>() != null)
                        {
                            var entity = hit.transform.GetComponent<Entity>();
                            foreach (var e in np.Selected.Value)
                            {
                                var job = new MoveInteractJob(e, entity);
                                e.ToDo.Enqueue(job);
                            }

                            CollectRegionJob.CollectRegionGroup(np.Selected.Value, startTile, endTile);
                        }
                        else
                        {
                            // TODO: decide who gets this job of selected, this is a temp fix
                            var spiral = Spiral(_myMapManager.GridIndexFromWorldPos(worldMousePos)).GetEnumerator();
                            
                            foreach (var current in np.Selected.Value)
                            {
                                if (np.myFactionID > -1 && np.Selected.Value != null)
                                {
                                    Vector2Int movePos = spiral.Current;
                                    while (!(movePos.x >= 0 && movePos.x < _myMapManager.MapWidth
                                                            && movePos.y >= 0 && movePos.y < _myMapManager.MapHeight
                                                            && _myMapManager[movePos] == Tile.Empty))
                                    {
                                        spiral.MoveNext();
                                        movePos = spiral.Current;
                                    }
                                    
                                    current.ToDo.Enqueue(new MoveJob(current, movePos));
                                    spiral.MoveNext();
                                }
                            }
                        }
                    }
                    else
                    {
                        var units = from e in np.Selected.Value where e.GetComponent<Unit>() != null select e.GetComponent<Unit>();

                        HashSet<Vector2Int> mineSet = new HashSet<Vector2Int>(tilesToMine);
                        HashSet<Entity> ores = new HashSet<Entity>();
                        var haulers = from unit in units where unit.Type == Constants.Entities.Hauler.ID select unit;
                        CollectRegionJob.CollectRegionGroup(haulers, startTile, endTile);
                        
                        foreach (Unit u in units)
                        {
                            if (u.Type != Constants.Entities.Hauler.ID) // if its not a truck
                                u.ToDo.Enqueue(new MineJob(u, mineSet));
//                            u.ToDo.Enqueue(new ActionJob(()=>GetCollectablesInRegion(startTile, endTile, ores)));
//                            Debug.Log("enquing collection");
//                            u.ToDo.Enqueue(new CollectionJob(u, ores));
                            //u.ToDo.Enqueue(new CollectRegionJob(u, startTile, endTile));
                        }
                    }
                }
                
                if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    foreach (var u in np.Selected.Value)
                    {
                        u.ForceNextJob();
                    } 
                }
            }
            
            // Scroll
            float d = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(d) > float.Epsilon)
            {
                if (_inverseScroll) d *= -1;
                if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
                _scrollRoutine = StartCoroutine(Scroll(d));
            }
            
            
            if (Math.Abs(Input.GetAxis("Focus")) > float.Epsilon && np.FrontSelected)
                _camPosition = np.FrontSelected.transform.position;

            if (Math.Abs(Input.GetAxis("Focus Home")) > float.Epsilon)
                _camPosition = np.Hub.transform.position;
            
            if (Math.Abs(Input.GetAxis("Collect Area")) > float.Epsilon && _prevCollect == 0)
            {
                //Debug.Log("Collect = " + _collectRegion);
                _collectRegion = !_collectRegion;
            }

            _prevCollect = Input.GetAxis("Collect Area");
        }


        private IEnumerator Scroll(float d)
        {
            d += 1;
            _scrollFactor *= d;
            _scrollFactor = Mathf.Clamp(_scrollFactor, _minSize, _maxSize);
            _myCamera.orthographicSize = _scrollFactor;
            yield return null;
        }

        private HashSet<Entity> GetCollectablesInRegion(Vector2Int startTile, Vector2Int endTile, HashSet<Entity> result = null)
        {
            NeoPlayer np = NeoPlayer.Instance;
            result = result ?? new HashSet<Entity>();
            
            for (int i = 0; i < np.FactionEntities.GetLength(1); i++)
            {
                var entity = np.FactionEntities[0, i];
                if (entity != null)
                {
                    var gridPos = np.MapManager.GridIndexFromWorldPos(entity.transform.position);
                    if (entity != null  && entity.factionID == 0 & 
                        gridPos.x <= Mathf.Max(startTile.x, endTile.x) && 
                        gridPos.x >= Mathf.Min(startTile.x, endTile.x) && 
                        gridPos.y <= Mathf.Max(startTile.y, endTile.y) && 
                        gridPos.y >= Mathf.Min(startTile.y, endTile.y))
                        result.Add(entity);
                }
            }
            //Debug.Log("ores: "  + result.Count);
            return result;
        }
        
        private IEnumerable<Vector2Int> Spiral(Vector2Int center)
        {
            Vector2Int[] directions = new Vector2Int[4]
            {
                new Vector2Int(0,1), new Vector2Int(1,0),
                new Vector2Int(0,-1), new Vector2Int(-1,0)
            };
            int dirIdx = 0;
            while (true)
            {
                Vector2Int dir = directions[dirIdx % 4];
                for (int i = 0; i <= dirIdx / 2; i++)
                {
                    yield return center;
                    center += dir;
                }
                dirIdx++;
            }
        }
    }
}
