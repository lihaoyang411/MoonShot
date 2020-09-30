using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TeamBlack.MoonShot.Networking;


namespace TeamBlack.MoonShot
{
    public class OreMap : MonoBehaviour
    {
        [SerializeField] private Tilemap _oreMap;
        [SerializeField] private Tilemap _blockMap;
        [SerializeField] private Tilemap _groundMap;
        [SerializeField] private MapManager _myMapManager;
        [SerializeField] private NetClient _client;
        public TileBase GetTile(int i, int j)
        {
            var pos = new Vector3Int(i, j, 0);
            return _blockMap.GetTile(pos) ??
                   _blockMap.GetTile(pos) ??
                   _groundMap.GetTile(pos);
        }

        public Vector3 cellSize => _blockMap.cellSize;

        private AudioManager _audioManager;
        void Awake()
        {
            _audioManager = GameObject.FindObjectOfType<AudioManager>();
            _myMapManager.OreMap = this;
        }
        
        public void SetTile(Vector3Int pos, Tile t)
        {
            switch (t)
            {
                case(Tile.Empty):

                    if(_blockMap.GetTile(pos) != _myMapManager.WallTiles[(int)Tile.Fog])
                        _audioManager.PlayDig();

                    _oreMap.SetTile(pos, null);
                    _blockMap.SetTile(pos, null);
                    _groundMap.SetTile(pos, _myMapManager.WallTiles[(int)Tile.Empty]);
                    _blockMap.SetColor(pos, Color.white);
                    _groundMap.SetColor(pos, Color.white);
                    _oreMap.SetColor(pos, Color.white);
                    break;
                case(Tile.Stone):
                    _oreMap.SetTile(pos, null);
                    _blockMap.SetTile(pos, _myMapManager.WallTiles[(int)t]);
                    _groundMap.SetTile(pos, _myMapManager.WallTiles[(int)Tile.Empty]);
                    break;
                case(Tile.Ore):
                    _oreMap.SetTile(pos, _myMapManager.WallTiles[(int)Tile.Ore]);
                    _blockMap.SetTile(pos, _myMapManager.WallTiles[(int)Tile.Stone]);
                    _groundMap.SetTile(pos, _myMapManager.WallTiles[(int)Tile.Empty]);
                    break;
                case (Tile.Fog):
                    _oreMap.SetTile(pos, null);
                    _blockMap.SetTile(pos, _myMapManager.WallTiles[(int)Tile.Fog]);
                    _groundMap.SetTile(pos, _myMapManager.WallTiles[(int)Tile.Empty]);
                    break;
                case (Tile.Gas):
                    _oreMap.SetTile(pos, null);
                    _blockMap.SetTile(pos, null);
                    _groundMap.SetTile(pos, _myMapManager.WallTiles[(int)Tile.Gas]);
                    break;
            }
        }
        
        public void SetTile(int i, int j, Tile t)
        {
            var pos = new Vector3Int(i, j, 0);
            SetTile(pos, t);
        }

        public Vector3Int WorldToCell(Vector3 pos)
        {
            var block = _blockMap.WorldToCell(pos);
            var ore = _oreMap.WorldToCell(pos);
            var ground = _groundMap.WorldToCell(pos);
            
            if (block != ground || block != ore) Debug.LogWarning("Tile maps not aligned with size");
            return block;
        }
        
        public Vector3 CellToWorld(Vector3Int pos)
        {
            var block = _blockMap.CellToWorld(pos);
            var ore = _oreMap.CellToWorld(pos);
            var ground = _groundMap.CellToWorld(pos);
            
            if (block != ground || block != ore) Debug.LogWarning("Tile maps not aligned with size");
            return block;
        }

        public void Blink(List<Vector2Int> tiles)
        {
            StartCoroutine(BlinkTiles(tiles));
        }

        public IEnumerator BlinkTiles(List<Vector2Int> tiles)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                yield return new WaitForSeconds(0.01f);
                SetTileColor(Color.green, tiles[i]);
                Object.FindObjectOfType<AudioManager>().PlayTileConfirm();
            }
        }

        private Tilemap[] GetApplicableTilemaps(Vector2Int pos)
        {
            List<Tilemap> maps = new List<Tilemap>();
            var cellPos = _myMapManager.IndexToCell(pos);
            Tile t = _myMapManager[pos];
            switch (t)
            {
                case Tile.Empty:
                    maps.Add(_groundMap);
                    break;
                case Tile.Fog:
                    // FIXME: fogmap?
                    break;
                case Tile.Stone:
                    maps.Add(_groundMap);
                    maps.Add(_blockMap);
                    break;
                case Tile.Ore:
                    maps.Add(_groundMap);
                    maps.Add(_blockMap);
                    maps.Add(_oreMap);
                    break;
                case (Tile.Gas):
                    maps.Add(_groundMap);
                    break;
                default:
                    Debug.LogError("unhandled orecase plz fix");
                    break;
            }

            return maps.ToArray();
        }
        
        public void SetTileColor(Color c, Vector2Int pos)
        {
            var cell = _myMapManager.IndexToCell(pos);
            var maps = GetApplicableTilemaps(pos);
            Color[] currColors = new Color[maps.Length];
            for (int i = 0; i < maps.Length; i++)
            {
                currColors[i] = maps[i].GetColor(cell);
                maps[i].SetColor(cell,c);
            }
        }
        
        public void SetTileColorFrame(Color c, Vector2Int pos)
        {
            var cell = _myMapManager.IndexToCell(pos);

            StartCoroutine(SetTileColorOneFrame(c, cell, GetApplicableTilemaps(pos)));
        }

        private IEnumerator SetTileColorOneFrame(Color c, Vector3Int pos, Tilemap[] maps)
        {
            //for (int i = 0; i < maps.Length; i++)
            //{
            //    maps[i].SetColor(pos,c);
            //}

            _blockMap.SetColor(pos, c);
            _groundMap.SetColor(pos, c);
            _oreMap.SetColor(pos, c);

            yield return  new WaitForEndOfFrame();

            //for (int i = 0; i < maps.Length; i++)
            //{
            //    maps[i].SetColor(pos,Color.white);
            //}

            _blockMap.SetColor(pos, Color.white);
            _groundMap.SetColor(pos, Color.white);
            _oreMap.SetColor(pos, Color.white);
        }
    }
}

