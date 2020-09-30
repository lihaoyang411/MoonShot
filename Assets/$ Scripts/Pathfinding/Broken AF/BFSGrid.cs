using System;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

namespace TeamBlack.MoonShot.Networking
{
    //TODO: fix out of range exceptions for literal grid edge click cases
    public class BFSGrid
    {
        private Queue<ByteVector2> _toScan;
        private byte[,] _map;
        private bool[,] _visited;
        private List<ByteVector2> _cover;

        private ByteVector2 _from;
        private int _maxDepth;

        public BFSGrid(byte[,] map, ByteVector2 from, int depth)
        {
            _from = from;
            _maxDepth = depth;

            _visited = new bool[map.GetLength(0), map.GetLength(0)];
            _toScan = new Queue<ByteVector2>();
            _cover = new List<ByteVector2>();

            _map = map;
            _toScan.Enqueue(from);
            _visited[from.Y, from.X] = true;

            while (_toScan.Count != 0)
            {
                ByteVector2 next = _toScan.Dequeue();
                _cover.Add(new ByteVector2(next.Y,next.X));

                if (_map[next.X, next.Y] == 0)
                {
                    TryEnqueue((byte)(next.X + 1), (byte)(next.Y));
                    TryEnqueue((byte)(next.X - 1), (byte)(next.Y));
                    TryEnqueue((byte)(next.X), (byte)(next.Y + 1));
                    TryEnqueue((byte)(next.X), (byte)(next.Y - 1));
                }
            }
        }

        private void TryEnqueue(byte x, byte y)
        {
            if (x < 0 || x >= _map.GetLength(0)
            || y < 0 || y >=  _map.GetLength(0))
                return;

            if (_visited[x, y])
                return;

            _visited[x, y] = true;

            ByteVector2 toEnqueue = new ByteVector2(x, y);
            if (ByteVector2.ManhattanDistance(_from, toEnqueue) > _maxDepth)
                return;

            _toScan.Enqueue(toEnqueue);
        }

        public ByteVector2[] GetCoverage()
        {
            return _cover.ToArray();
        }
    }

  
}
