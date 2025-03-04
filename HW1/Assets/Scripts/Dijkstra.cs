using System.Diagnostics;
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
    public static Color pathColor = Color.yellow;

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

        // ------------------------------------------------------------------------------------------------------
        // start(From-->GraphInterface)-->Has a Node Script 
        // end(To-->GraphInterface)-->Has a Node Script

        // pathfinding list is a specialized data structure that acts
        // very much like a regular list. It holds a set of NodeRecord

        // TODO Be sure to include the Debug and Yield statements to get the right outputs in Unity.
        // ------------------------------------------------------------------------------------------------------


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

        // For coloring
        SpriteRenderer renderer = start.GetComponentInChildren<SpriteRenderer>();

        NodeRecord current = new NodeRecord();
        Dictionary<Direction, GameObject> connections = new Dictionary<Direction, GameObject>();
        // --Iterate through processing each node.--
        while (open.Count > 0)
        {

            // --Find the smallest element in the open list.--
            current = SmallestElement(open);
            // --If coloring tiles, update the tile color.--
            if (colorTiles)
            {
                // Grab the renderer of the clicked tile.
                //UnityEngine.Debug.Log("Search Failed render");
                //from = hit.collider.gameObject;
                renderer = current.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                // Turn the tile color to magenta to visualize the selection.
                renderer.material.color = activeColor;
            }

            // --Pause the animation to show the new active tile.--
            yield return new WaitForSeconds(waitTime);

            // --If it is the goal node, then terminate.--
            if (current.node == end.GetComponent<Node>())
            {
                print("Current Node == End Node");
                break;
            }

            // --Otherwise get its outgoing connections.--
            connections = current.node.Connections;
            print("connections Count: " + connections.Count);
            NodeRecord endNodeRecord = null;
            foreach (GameObject connection in connections.Values)
            {
                print("Open: " + open.Count + "\nClosed: " + closed.Count);
                // --Get the cost estimate for the end node.--
                Node endNode = connection.GetComponent<Node>();
                float endNodeCost = current.costSoFar + 1f; // connection.getCost() = 1  TODO figure out how to use a cost per Node


                // -- Skip if the node is closed
                if (Contains(closed, endNode))
                {
                    //endNodeRecord.costSoFar = endNodeCost;
                    //endNodeRecord.Tile = current.Tile; // Keep track of the path
                    continue;
                }
                // --.. or if it is open and we�ve found a worse route.--
                else if (Contains(open, endNode))
                {
                    // -- Here we find the record in the open list --
                    // -- corresponding to the endNode. --
                    endNodeRecord = Find(open, endNode);
                    endNodeRecord.node = endNode;
                    if (endNodeRecord.costSoFar <= endNodeCost) // TODO This may cause an error... endNodeRecord.costSoFar != endNodeRecord.cost in the book
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
                endNodeRecord.costSoFar = endNodeCost;
                // endNodeRecord.connection = connection; TODO this may cause some error...

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

                // -- We�ve finished looking at the connections for the current --
                // -- node, so add it to the closed list and remove it from the --
                // -- open list. --


                // --If coloring tiles, update the closed tile color.--
                if (colorTiles)
                {
                    // Grab the renderer of the clicked tile.
                    renderer = current.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                    // Turn the tile color to magenta to visualize the selection.
                    renderer.material.color = closedColor;
                }

            }

            open.Remove(current);
            closed.Add(current);
        }

   


        // Stops the stopwatch.
        watch.Stop();

        UnityEngine.Debug.Log("Seconds Elapsed: " + (watch.ElapsedMilliseconds / 1000f).ToString());
        UnityEngine.Debug.Log("Nodes Expanded: " + "print the number of nodes expanded here.");

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
            // --Work back along the path, accumulating connections.--
            //while (current.node != start)
            //{

            //}
            //UnityEngine.Debug.Log("Path Length: " + path.Count.ToString());
            UnityEngine.Debug.Log("Path Length: 0");
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

/// <summary>
/// A class for recording search statistics.
/// </summary>
public class NodeRecord
{
    // The tile game object.
    public GameObject Tile { get; set; } = null;
    // Set the other class properties here.
    public Node node { get; set; } = null;
    public float costSoFar { get; set; } = 0;
    public float cost { get; set; } = 1;
    private Dictionary<Direction, NodeRecord> connections = new Dictionary<Direction, NodeRecord>();

    public Dictionary<Direction, GameObject> getConnections()
    {
        return node.Connections;
    }

    /*    public Dictionary<Direction, GameObject> Connections()
        {
            return node.Connections;//new Dictionary<Direction, GameObject>();
        }*/

    /*// Grab the node scripts attached to the two tile game objects.
    Node fromNode = from.GetComponent<Node>();
    Node toNode = to.GetComponent<Node>();

    // A method for debugging connections.
    // Also demonstrated how to enumerate the connected game world tiles in Connections.
    public void printConnections()
    {
        // Iterates through the different values in the direction enum (Up, Down, Left, Right).
        foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            // If there is a connection in that direction, prints this tile's name, the connection direction, and the connected tile's name.
            if (Connections.ContainsKey(direction)) Debug.Log(name + " " + direction + " " + Connections[direction].name);
    }*/

    // Sets the tile's color.
    public void ColorTile(Color newColor)
    {
        SpriteRenderer renderer = Tile.GetComponentInChildren<SpriteRenderer>();
        renderer.material.color = newColor;
    }

    // Displays a string on the tile.
    public void Display(float value)
    {
        TextMesh text = Tile.GetComponent<TextMesh>();
        text.text = value.ToString();
    }
}