using System;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

namespace TeamBlack.MoonShot.Networking
{
    //TODO: fix out of range exceptions for literal grid edge click cases
    public class DFSGrid
    {
        private Queue<ByteVector2> _toScan;
        private byte[,] _map;
        private bool[,] _visited;
        private List<ByteVector2> _cover;
        private int _depthBound;

        public DFSGrid(byte[,] map, ByteVector2 from, int depthBound)
        {
            _visited = new bool[map.GetLength(0), map.GetLength(0)];
            _toScan = new Queue<ByteVector2>();
            _cover = new List<ByteVector2>();

            // Check params
            if (map[from.Y, from.X] > 0)
            {
                Debug.Log("ERROR: Cannot path to full space");
                return;
            }
            if (depthBound <= 0)
            {
                Debug.Log("ERROR: Cannot do search of depth <= 0");
                return;
            }

            _map = map;
            _depthBound = depthBound;

            DFSHelper(from.Y, from.X, 0);
            
        }

        private void DFSHelper(byte x, byte y, int depth)
        {
            // Base case
            //Debug.Log($"DFS HELP: {depth > _depthBound} {_visited[x, y] == true} {(int)_map[x, y] > 0}");
            if (depth > _depthBound || _visited[x, y] == true)
            {
                //Debug.Log("RETURNING!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                return;
            }

            _visited[x, y] = true;
            _cover.Add(new ByteVector2(x, y));

            DFSHelper((byte)(x + 1), (byte)(y), depth + 1);
            DFSHelper((byte)(x - 1), (byte)(y), depth + 1);
            DFSHelper((byte)(x), (byte)(y + 1), depth + 1);
            DFSHelper((byte)(x), (byte)(y - 1), depth + 1);
        }

        public ByteVector2[] GetCoverage()
        {
            //Debug.Log("RETURNING " + _cover.Count);
            return _cover.ToArray();
        }
    }


}
