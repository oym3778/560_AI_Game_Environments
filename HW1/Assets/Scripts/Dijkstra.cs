﻿using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using System.Xml.Linq;
using System;
using Unity.Burst.CompilerServices;
using UnityEditor.MemoryProfiler;

/// <summary>
/// Performs search using Dijkstra's algorithm.
/// </summary>
public class Dijkstra : MonoBehaviour
{
    // Colors for the different search categories.
    public static Color openColor = Color.cyan;
    public static Color closedColor = Color.blue;
    public static Color activeColor = Color.yellow;
    public static Color pathColor = Color.green;

    // The stopwatch for timing search.
    private static Stopwatch watch = new Stopwatch();

    public static IEnumerator search(GameObject start,
                                     GameObject end,
                                     float waitTime,
                                     bool colorTiles = false,
                                     bool displayCosts = false,
                                     Stack<NodeRecord> path = null)
    {
        // Starts the stopwatch.
        watch.Start();

        // Add your Dijkstra code here.

        int nodesExpanded = 0;

        // --Initialize the record for the start node.--
        NodeRecord startRecord = new NodeRecord();
        startRecord.Tile = start;
        startRecord.node = start.GetComponent<Node>();
        // Since we can access the current nodes connections through the
        // Node class we just use a Connections Method in Node records
        // startRecord.connection = null
        startRecord.costSoFar = 0;

        // --Initialize the open and closed lists.--
        List<NodeRecord> open = new List<NodeRecord>();
        open.Add(startRecord);
        List<NodeRecord> closed = new List<NodeRecord>();
        nodesExpanded++;
        
        // For coloring
        SpriteRenderer renderer = start.GetComponentInChildren<SpriteRenderer>();

        NodeRecord current = new NodeRecord();
        Dictionary<Direction, GameObject> connections = new Dictionary<Direction, GameObject>();
        // --Iterate through processing each node.--
        while (open.Count > 0)
        {
            //nodesExpanded++;
            // --Find the smallest element in the open list.--
            current = SmallestElement(open);
            current.Tile = current.node.gameObject;
            // --If coloring tiles, update the tile color.--
            if (colorTiles)
            {
                // Grab the renderer of the clicked tile.
                //UnityEngine.Debug.Log("Search Failed render");
                //from = hit.collider.gameObject;
                renderer = current.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                renderer.material.color = activeColor;
            }

            // --Pause the animation to show the new active tile.--
            yield return new WaitForSeconds(waitTime);

            // --If it is the goal node, then terminate.--
            if (current.node == end.GetComponent<Node>())
            {
                break;
            }

            // --Otherwise get its outgoing connections.--
            connections = current.node.Connections;
            
            foreach (GameObject connection in connections.Values)
            {
                
                NodeRecord endNodeRecord = null;
                // --Get the cost estimate for the end node.--
                Node endNode = connection.GetComponent<Node>(); // this is the to node
                // add a from node
                float endNodeCost = current.costSoFar + 1f;


                // -- Skip if the node is closed
                if (Contains(closed, endNode))
                {
                    continue;
                }
                // --.. or if it is open and we�ve found a worse route.--
                else if (Contains(open, endNode))
                {
                    // -- Here we find the record in the open list --
                    // -- corresponding to the endNode. --
                    endNodeRecord = Find(open, endNode);
                    if (1f <= endNodeCost)
                    {
                        continue;
                    }
                }
                // --Otherwise we know we�ve got an unvisited node,--
                // --so make a record for it.--
                else
                {
                    endNodeRecord = new NodeRecord();
                    endNodeRecord.node = endNode;
                }

                // -- We�re here if we need to update the node. Update the --
                // -- cost and connection. --
                nodesExpanded++;
                endNodeRecord.costSoFar = endNodeCost;              
                endNodeRecord.fromNode = current; //TODO this may cause some error...

                // --If displaying costs, update the tile display.--
                if (displayCosts)
                {
                    // Grab the text mesh off the tile.
                    TextMesh text = connection.GetComponent<TextMesh>();

                    // Reset its text to be blank.
                    text.text = "" + endNodeCost;
                }

                // --And add it to the open list.--
                if (!Contains(open, endNode))
                {
                    open.Add(endNodeRecord);
                }

                // --If coloring tiles, update the open tile color.--
                if (colorTiles)
                {
                    // Grab the renderer of the clicked tile.
                    renderer = endNodeRecord.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                    // Turn the tile color to magenta to visualize the selection.
                    renderer.material.color = openColor;
                }
                // --Pause the animation to show the new open tile.--
                yield return new WaitForSeconds(waitTime);

            }

            // -- We�ve finished looking at the connections for the current --
            // -- node, so add it to the closed list and remove it from the --
            // -- open list. --
            open.Remove(current);
            closed.Add(current);

            // --If coloring tiles, update the closed tile color.--
            if (colorTiles)
            {
                // Grab the renderer of the clicked tile.
                renderer = current.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                // Turn the tile color to magenta to visualize the selection.
                renderer.material.color = closedColor;
            }
        }




        // Stops the stopwatch.
        watch.Stop();

        UnityEngine.Debug.Log("Seconds Elapsed: " + (watch.ElapsedMilliseconds / 1000f).ToString());
        UnityEngine.Debug.Log("Nodes Expanded: " + nodesExpanded);

        // Reset the stopwatch.
        watch.Reset();

        // Determine whether Dijkstra found a path and print it here.
        // --We�re here if we�ve either found the goal, or if we�ve no more --
        // --nodes to search, find which. --
        if (current.node != end.GetComponent<Node>())
        {
            // --We've run out of nodes without finding the goal,--
            // --so there�s no solution.--
            UnityEngine.Debug.Log("Search Failed");
        }
        else
        {
            //path = new Stack<NodeRecord>();
            // --Work back along the path, accumulating connections.--
            path.Push(current);
            while (current.node != start.GetComponent<Node>())
            {
                if (current.node == null) { UnityEngine.Debug.Log("Current node looping back got null, something wrong with fromNode"); }
                path.Push(current.fromNode);
                current = current.fromNode;


                // --If coloring tiles, update the open tile color.--
                if (colorTiles)
                {
                    // Grab the renderer of the clicked tile.
                    renderer = current.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                    // Turn the tile color to magenta to visualize the selection.
                    renderer.material.color = pathColor;
                }

                // -- Pause the animation to show the new path tile.
                // --This is the actual C# command to use.
                yield return new WaitForSeconds(waitTime);
            }
            //UnityEngine.Debug.Log("Path Length: " + path.Count.ToString());
            UnityEngine.Debug.Log("Path Length: " + path.Count.ToString());
        }

        yield return null;
    }

    /// <summary>
    /// The smallestElement method returns the NodeRecord structure in the list 
    /// with the lowest costSoFar value.
    /// </summary>
    public static NodeRecord SmallestElement(List<NodeRecord> records)
    {
        float lowestCost = records[0].costSoFar;
        NodeRecord lowestNodeRecord = records[0];

        foreach (NodeRecord nodeRecord in records)
        {
            if (nodeRecord.costSoFar < lowestCost)
            {
                lowestCost = nodeRecord.costSoFar;
                lowestNodeRecord = nodeRecord;
            }
            // code block to be executed
        }

        // look through the open list and return the NodeRecord with the lowest cost
        return lowestNodeRecord;
    }

    /// <summary>
    /// The contains(node) method returns true only if the list contains a 
    /// NodeRecord structure whose node member is equal to the given parameter.
    /// <param name="node"> A Node Script
    /// </summary>
    public static bool Contains(List<NodeRecord> records, Node node)
    {
        // if the given node is within the open list return true
        foreach (NodeRecord nodeRecord in records)
        {
            if (nodeRecord.node == node)
            {
                return true;
            }
        }

        // else the node wasnt in the list, return false
        return false;
    }

    /// <summary>
    /// The find(node) method returns the NodeRecord structure from the list 
    /// whose node member is equal to the given parameter.
    /// </summary>
    public static NodeRecord Find(List<NodeRecord> records, Node node)
    {
        foreach (NodeRecord nodeRecord in records)
        {
            if (nodeRecord.node == node)
            {
                return nodeRecord;
            }
        }

        // there was no node within the records...
        return null;
    }


}
