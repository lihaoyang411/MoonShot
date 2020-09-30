using System.Collections;
using System.Collections.Generic;
using System;
using Priority_Queue;
using UnityEngine;
using System.Diagnostics;

public class Wackfinder: MonoBehaviour
{
    public GameObject frontierMarker;
    public GameObject pathMarker;

    public struct Coord2D 
    {
        public int x;
        public int y;

        public Coord2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class TileNode: FastPriorityQueueNode
    {   
        public bool closed = false;

        // Location of the node
        public int x;
        public int y;

        // Estimated distance from start to end node (inc. this tile)
        public int totalCost;

        // Distance from start node
        public int distanceFromStart;

        // Estimated distance to end node from here
        public int heuristic;

        // Pointer to parent for storing best path
        public TileNode parent;

        // Track success
        public TileNode(int x, int y, int distanceFromStart, int heuristic)
        {
            this.x = x;
            this.y = y;
            this.distanceFromStart = distanceFromStart;
            this.heuristic = heuristic;
            RecalculateTotalCost();
        }

        public TileNode(TileNode toCopy)
        {
            this.x = toCopy.x;
            this.y = toCopy.y;
            this.distanceFromStart = toCopy.distanceFromStart;
            this.heuristic = toCopy.heuristic;
            this.totalCost = toCopy.totalCost;
            this.parent = toCopy.parent;
            this.closed = toCopy.closed;
        }

        public void RecalculateTotalCost()
        {
            this.totalCost = this.distanceFromStart + this.heuristic;
        }

        
    }

    // Goal
    private Coord2D destination;
    private bool finished = false;

    // Holds collision data for traversable map
    public byte[,] byteMap;
    // Tracks open and closed nodes
    public TileNode[,] nodeTracker;
    // Supplies best open node
    FastPriorityQueue<TileNode> nodeQueue;

    // TODO: convert back to constructor
    public void WackfinderINIT(byte[,] map, Coord2D from, Coord2D to)
    {   
        print("Called INIT");

        if(map[from.y, from.x] != 0) 
        {
            print("ERROR CAN MOVE FROM THAT SPACE!");
            return;
        }
        if (map[to.y, to.x] != 0)
        {
            print("ERROR CAN MOVE TO THAT SPACE!");
            return;
        }

        destination = to;
        byteMap = map;
        nodeTracker = new TileNode[map.GetLength(0), map.GetLength(0)];
        nodeQueue = new FastPriorityQueue<TileNode>(map.GetLength(0) * map.GetLength(0));

        AStar(from, to);
    }

    public void AStar(Coord2D from, Coord2D to)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        // Initialize start node
        int initialHeuristic = EuclideanDistance(from, to);
        TileNode startNode = new TileNode(from.x, from.y, 0, initialHeuristic);

        // Store start node
        nodeQueue.Enqueue(startNode, startNode.totalCost);
        nodeTracker[startNode.y, startNode.x] = startNode;

        int timeC = 0;

        while(true)
        {
//            timeC++;
//            if (timeC > 14)

//            {
//                yield return null;
//                timeC = 0;
//            }

            if (nodeQueue.Count == 0) // If the queue is empty, exit and fail
            {
                print("UNABLE TO FIND PATH!");
                break;
            }

            TileNode bestNode = nodeQueue.Dequeue();

            transform.position = new Vector3(bestNode.x, bestNode.y, 1);
            GameObject.Instantiate(frontierMarker).transform.position = transform.position;

 
            // Set closed state and break if dest. reached
            if(SetClosedState(bestNode))
            {
                ShowBestPath(bestNode);
                break;
            }

            AScanAdjacent(bestNode);
            
        }

        stopWatch.Stop();
        
        // Get the elapsed time as a TimeSpan value.
        TimeSpan ts = stopWatch.Elapsed;

        // Format and display the TimeSpan value.
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
        print("RunTime " + elapsedTime);
    }

    public void AScanAdjacent(TileNode node)
    {
        if(node.x - 1 >= 0) // Scan left
        {
            AStarScan(new Coord2D(node.x - 1, node.y), node);
        }
        if(node.x + 1 < nodeTracker.GetLength(0)) // Scan right
        {
            AStarScan(new Coord2D(node.x + 1, node.y), node);
        }
        if(node.y - 1 >= 0) // Scan up
        {
            AStarScan(new Coord2D(node.x, node.y - 1), node);
        }
        if(node.y + 1 < nodeTracker.GetLength(0)) // Scan down
        {
            AStarScan(new Coord2D(node.x, node.y + 1), node);
        }
    }

    void AStarScan(Coord2D here, TileNode toParent)
    {

        TileNode scanNode = nodeTracker[here.y, here.x];

        // If the tile is not traversable, ignore
        if(byteMap[here.y, here.x] != 0)
        {
            return;
        }
        // If the tile is traversable and stateless
        else if(scanNode == null)
        {
            // Initialize and store a new open tile node
            int heuristic = EuclideanDistance(here, destination);
            TileNode newOpenNode = new TileNode(here.x, here.y, toParent.distanceFromStart+1, heuristic);
            newOpenNode.parent = toParent;
            nodeTracker[here.y, here.x] = newOpenNode;
            nodeQueue.Enqueue(newOpenNode, newOpenNode.totalCost);
        }
        // If the tile is closed, ignore
        else if(scanNode.closed)
        {
            return;
        }
        // If the tile is already open...
        else if(!scanNode.closed)
        {   
            // If new path to this node is better...
            if(scanNode.distanceFromStart > toParent.distanceFromStart)
            {   
                // Recalculate total cost and requeue
                scanNode.distanceFromStart = toParent.distanceFromStart + 1;
                scanNode.RecalculateTotalCost();
                scanNode.parent = toParent;
                nodeQueue.UpdatePriority(scanNode, scanNode.totalCost);
            }
        }

        return;
    }

    public int EuclideanDistance(Coord2D from, Coord2D to)
    {
        return (int)
            Math.Sqrt(
                Math.Pow(Math.Abs(from.x - to.x), 2) + 
                Math.Pow(Math.Abs(from.y - to.y), 2)
                );
    }

    private bool SetClosedState(TileNode node)
    {
        node.closed = true;
        if(node.x == destination.x && node.y == destination.y)
        {
            return true;
        }

        return false;
    }

    private void ShowBestPath(TileNode fromNode)
    {
        while (fromNode.parent != null)
        {
            transform.position = new Vector3(fromNode.x, fromNode.y, 1);
            GameObject.Instantiate(pathMarker).transform.position = transform.position;
            fromNode = fromNode.parent;
        }
    }



    
    
}
