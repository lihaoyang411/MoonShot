using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Threading;
using UnityEngine;

namespace TeamBlack.MoonShot.Networking
{
    public class ServerPathfinder
    {
        private byte[,] byteMap;

        // Update the current memory model of the world space
        public void SetByteMap(Tile[,] map)
        {
            byteMap = new byte[map.GetLength(0), map.GetLength(0)];

            for (int y = 0; y < map.GetLength(0); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    byteMap[y, x] = (byte)map[x, y];
                    if (byteMap[y, x] == (byte)Tile.Gas) byteMap[y, x] = 0; // Filter out gas :/
                }
            }
        }

        public ByteVector2[] FindCoverage(ByteVector2 from, int depth)
        {
            BFSGrid bfs = new BFSGrid(byteMap, new ByteVector2(from.Y, from.X), depth);
            return bfs.GetCoverage();
        }

        // You can implicitly convert Vector2s to Vector3s
        public ByteVector2[] FindBestPath(ByteVector2 from, ByteVector2 to)
        {
            ByteVector2[] toReturn = new ByteVector2[0];
            Thread pathThread = new Thread(() => FindBestPathProcess(ref toReturn, from, to));
            pathThread.Start();
            pathThread.Join();

            return toReturn;
        }

        public ByteVector2[] FindBestAdjacentPath(ByteVector2 from, ByteVector2 to)
        {
            ByteVector2[] toReturn = new ByteVector2[0];
            Thread pathThread = new Thread(() => FindBestAdjacentPathProcess(ref toReturn, from, to));
            pathThread.Start();
            pathThread.Join();
            return toReturn;
        }



        // On @on, finds the optimal path @path to one of the tiles adjacent to @to from @from 
        // This is useful when selecting a tile for a mining path
        private void FindBestAdjacentPathProcess(ref ByteVector2[] path, ByteVector2 from, ByteVector2 to)
        {
            ByteVector2[] upPath    = new ByteVector2[0];
            ByteVector2[] downPath  = new ByteVector2[0];
            ByteVector2[] leftPath  = new ByteVector2[0];
            ByteVector2[] rightPath = new ByteVector2[0];

            // Fix Me: verify that these lines work since I changed the vector math implementation
            Thread scanUp = new Thread(() => FindBestPathProcess(ref upPath, from, to + new ByteVector2(0, 1)));
            Thread scanDown = new Thread(() => FindBestPathProcess(ref downPath, from, to - new ByteVector2(0, 1)));
            Thread scanLeft = new Thread(() => FindBestPathProcess(ref leftPath, from, to + new ByteVector2(1, 0)));
            Thread scanRight = new Thread(() => FindBestPathProcess(ref rightPath, from, to - new ByteVector2(1, 0)));

            scanUp.Start();
            scanDown.Start();
            scanLeft.Start();
            scanRight.Start();

            scanUp.Join();
            scanDown.Join();
            scanLeft.Join();
            scanRight.Join();

            List<ByteVector2[]> adjacentPaths = new List<ByteVector2[]>(4);
            adjacentPaths.Add(upPath);
            adjacentPaths.Add(downPath);
            adjacentPaths.Add(leftPath);
            adjacentPaths.Add(rightPath);
            adjacentPaths.Sort((a, b) => {

                int aLength = int.MaxValue;
                if (a != null)
                    aLength = a.Length;
                int bLength = int.MaxValue;
                if (b != null)
                    bLength = b.Length;
                return aLength - bLength;
            });

            path = adjacentPaths[0];
        }

        // Finds the optimal path @path on @on from @from to @to
        // You can implicitly convert Vector2s to Vector3s (Vector2.Vector3)
        // This method is now designed to run on its own thread (i.e. join() then read path)
        public void FindBestPathProcess(ref ByteVector2[] path, ByteVector2 from, ByteVector2 to)
        {
            // For benchmarking
            // Stopwatch stopWatch = new Stopwatch();
            // stopWatch.Start();

            // Create and run an instance of the A* algo
            AStarGrid gridInstance = new AStarGrid(
                byteMap,
                new ByteVector2(from.X, from.Y),
                new ByteVector2(to.X, to.Y));

            ByteVector2[] coordPath = gridInstance.GetBestPath();

            // Record benchmark
            // stopWatch.Stop();
            // TimeSpan ts = stopWatch.Elapsed;
            // string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //     ts.Hours, ts.Minutes, ts.Seconds,
            //     ts.Milliseconds / 10);
            // print("RunTime " + elapsedTime);

            // Cast coords to vect2 path
            path = null;
            if (coordPath != null)
            {
                path = new ByteVector2[coordPath.Length];
                for (int i = 0; i < coordPath.Length; i++)
                {
                    path[i] = new ByteVector2(coordPath[i].X, coordPath[i].Y);
                }
            }

            // Done (thread exits)
        }
    }
}
