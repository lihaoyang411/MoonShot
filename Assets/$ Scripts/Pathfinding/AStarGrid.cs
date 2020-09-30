using System;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

namespace TeamBlack.MoonShot.Networking
{
    //TODO: fix out of range exceptions for literal grid edge click cases
    public class AStarGrid
    {

        // Tracks a tile on the "open list" and the priority heap
        public class TileNode : FastPriorityQueueNode
        {
            // State prevents revisiting processed nodes
            public bool Closed { get; set; }

            // Location of the node
            public byte X { get; }
            public byte Y { get; }

            // Estimated distance from start to end node (inc. this tile)
            public int TotalCost { get; set; }

            // Distance from start node
            public int DistanceFromStart { get; set; }

            // Estimated distance to end node from here
            public int Heuristic { get; }

            // Pointer to parent for storing best path
            public TileNode Parent { get; set; }

            // Basic constructor
            public TileNode(byte x, byte y, int distanceFromStart, int heuristic)
            {
                this.X = x;
                this.Y = y;
                this.DistanceFromStart = distanceFromStart;
                this.Heuristic = heuristic;
                RecalculateTotalCost();
            }

            // Called if distanceFromStart changes
            public void RecalculateTotalCost()
            {
                this.TotalCost = this.DistanceFromStart + this.Heuristic;
            }
        }

        // Goal
        private ByteVector2 destination;
        private bool finished = false;

        // Holds collision data for traversable map
        public byte[,] byteMap;
        // Tracks open and closed nodes
        public TileNode[,] nodeTracker;
        // Supplies best open node
        FastPriorityQueue<TileNode> nodeQueue;
        private ByteVector2[] bestPath;

        public string debug;

        public AStarGrid(byte[,] map, ByteVector2 from, ByteVector2 to)
        {

            // Check params
            if (map[from.Y, from.X] != 0)
            {
                Console.WriteLine("ERROR: Cannot path to full space");
                return;
            }
            if (map[to.Y, to.X] != 0)
            {
                Console.WriteLine("ERROR: Cannot path from full space.");
                return;
            }

            // Initialize grid state
            // Invert destination for 'cavern optimization'
            destination = from;
            byteMap = map;
            nodeTracker = new TileNode[map.GetLength(0), map.GetLength(0)];
            nodeQueue = new FastPriorityQueue<TileNode>(map.GetLength(0) * map.GetLength(0));

            // Calculate shortest path
            // Start from destination for 'cavern optimization' -- prevents scanning of larger territories 
            AStar(to, from);
        }

        private void AStar(ByteVector2 from, ByteVector2 to)
        {
            // Initialize start node
            int initialHeuristic = EuclideanDistance(from, to);

            TileNode startNode = new TileNode(from.X, from.Y, 0, initialHeuristic);

            // Store start node
            nodeQueue.Enqueue(startNode, startNode.TotalCost);
            nodeTracker[startNode.Y, startNode.X] = startNode;

            // Run A*
            while (true)
            {
                if (nodeQueue.Count == 0) // If the queue is empty, exit and fail
                {
                    break;
                }

                // Get the next best node
                TileNode bestNode = nodeQueue.Dequeue();

                // Set closed state and break if dest. reached
                if (SetClosedState(bestNode))
                {
                    SetBestPath(bestNode);
                    break;
                }

                // Scan all adjacent nodes
                AStarScanAdjacent(bestNode);
            }
        }

        // Scan adjecent nodes to open or update them with new path data
        private void AStarScanAdjacent(TileNode node)
        {
            if (node.X - 1 >= 0) // Scan left
            {
                AStarScan(new ByteVector2((byte)(node.X - 1), node.Y), node);
            }
            if (node.X + 1 < nodeTracker.GetLength(0)) // Scan right
            {
                AStarScan(new ByteVector2((byte)(node.X + 1), node.Y), node);
            }
            if (node.Y - 1 >= 0) // Scan up
            {
                AStarScan(new ByteVector2(node.X, (byte)(node.Y - 1)), node);
            }
            if (node.Y + 1 < nodeTracker.GetLength(0)) // Scan down
            {
                AStarScan(new ByteVector2(node.X, (byte)(node.Y + 1)), node);
            }

            // (ignores walls)
        }

        // Scan a single tile, update its state accordingly
        private void AStarScan(ByteVector2 here, TileNode toParent)
        {
            // The tile to scan
            TileNode scanNode = nodeTracker[here.Y, here.X];

            // If the tile is not traversable, ignore
            if (byteMap[here.Y, here.X] != 0)
            {
                return;
            }
            // If the tile is traversable and stateless
            else if (scanNode == null)
            {
                // Initialize and store a new open tile node
                int heuristic = EuclideanDistance(here, destination);
                TileNode newOpenNode = new TileNode(here.X, here.Y, toParent.DistanceFromStart + 1, heuristic);
                newOpenNode.Parent = toParent;
                nodeTracker[here.Y, here.X] = newOpenNode;
                nodeQueue.Enqueue(newOpenNode, newOpenNode.TotalCost);
            }
            // If the tile is closed, ignore
            else if (scanNode.Closed)
            {
                return;
            }
            // If the tile is already open...
            else if (!scanNode.Closed)
            {
                // If new path to this node is better...
                if (scanNode.DistanceFromStart > toParent.DistanceFromStart)
                {
                    // Recalculate total cost and requeue
                    scanNode.DistanceFromStart = toParent.DistanceFromStart + 1;
                    scanNode.RecalculateTotalCost();
                    scanNode.Parent = toParent;
                    nodeQueue.UpdatePriority(scanNode, scanNode.TotalCost);
                }
            }
            else
            {
                Console.WriteLine("Error: invalid tile scan state reached");
            }

            return;
        }

        // Cost estimate heuristic based on shortest possible travel distance
        private int EuclideanDistance(ByteVector2 from, ByteVector2 to)
        {
            // I.e. pythagorean thm.
            return (int)
                Math.Sqrt(
                    Math.Pow(Math.Abs(from.X - to.X), 2) +
                    Math.Pow(Math.Abs(from.Y - to.Y), 2)
                    );
        }

        // Sets a nodes state to closed a checks for end state (I.e. destination reached)
        private bool SetClosedState(TileNode node)
        {
            node.Closed = true;
            if (node.X == destination.X && node.Y == destination.Y)
            {
                return true;
            }

            return false;
        }

        // Traces back through nodes from dest. to identify optimal path
        private void SetBestPath(TileNode fromNode)
        {
            LinkedList<ByteVector2> path = new LinkedList<ByteVector2>();
            int length = 0;

            do
            {
                length++;
                // Sort backwards to compensate for 'cavern optimization'
                path.AddLast(new ByteVector2(fromNode.X, fromNode.Y));
                fromNode = fromNode.Parent;
            } while (fromNode != null);

            ByteVector2[] toSet = new ByteVector2[length];
            path.CopyTo(toSet, 0);
            bestPath = toSet;
        }

        public ByteVector2[] GetBestPath()
        {
            return bestPath;
        }

    }
}
