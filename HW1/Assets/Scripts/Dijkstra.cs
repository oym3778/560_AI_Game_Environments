using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

/// <summary>
/// Performs search using Dijkstra's algorithm.
/// </summary>
public class Dijkstra : MonoBehaviour
{
    public static Color openColor = Color.cyan;
    public static Color closedColor = Color.blue;
    public static Color activeColor = Color.yellow;
    public static Color pathColor = Color.green;

    private static Stopwatch watch = new Stopwatch();

    public static IEnumerator search(GameObject start,
                                     GameObject end,
                                     float waitTime,
                                     bool colorTiles = false,
                                     bool displayCosts = false,
                                     Stack<NodeRecord> path = null)
    {
        watch.Start();

        Node startNode = start.GetComponent<Node>();
        Node endNode = end.GetComponent<Node>();

        if (startNode == null || endNode == null)
        {
            UnityEngine.Debug.LogError("Start or End GameObject does not contain a Node component!");
            yield break;
        }

        NodeRecord startRecord = new NodeRecord
        {
            Tile = start,
            node = startNode,
            costSoFar = 0,
            previousNode = null
        };

        List<NodeRecord> open = new List<NodeRecord> { startRecord };
        List<NodeRecord> closed = new List<NodeRecord>();

        SpriteRenderer renderer;
        NodeRecord current;

        while (open.Count > 0)
        {
            current = SmallestElement(open);
            if (current == null) break;

            if (colorTiles)
            {
                renderer = current.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                renderer.material.color = activeColor;
            }

            yield return new WaitForSeconds(waitTime);

            if (current.node == endNode)
            {
                UnityEngine.Debug.Log("Goal Found!");
                break;
            }

            foreach (var connection in current.node.Connections)
            {
                GameObject connectedObject = connection.Value;
                Node neighborNode = connectedObject.GetComponent<Node>();

                if (neighborNode == null)
                {
                    UnityEngine.Debug.LogWarning("A connected GameObject does not have a Node component!");
                    continue;
                }

                float newCost = current.costSoFar + 1f;  // Assuming a uniform movement cost

                if (Contains(closed, neighborNode))
                {
                    continue;
                }

                NodeRecord neighborRecord = Find(open, neighborNode);
                if (neighborRecord != null)
                {
                    if (neighborRecord.costSoFar <= newCost)
                    {
                        continue;
                    }
                }
                else
                {
                    neighborRecord = new NodeRecord { node = neighborNode };
                }

                neighborRecord.costSoFar = newCost;
                neighborRecord.previousNode = current.node;

                if (displayCosts)
                {
                    TextMesh text = connectedObject.GetComponent<TextMesh>();
                    if (text != null) text.text = newCost.ToString();
                }

                if (!Contains(open, neighborNode))
                {
                    open.Add(neighborRecord);
                }

                if (colorTiles)
                {
                    renderer = neighborRecord.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                    renderer.material.color = openColor;
                }

                yield return new WaitForSeconds(waitTime);
            }

            open.Remove(current);
            closed.Add(current);

            if (colorTiles)
            {
                renderer = current.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                renderer.material.color = closedColor;
            }
        }

        watch.Stop();
        UnityEngine.Debug.Log($"Seconds Elapsed: {watch.ElapsedMilliseconds / 1000f}");
        watch.Reset();

        if (closed.Find(nr => nr.node == endNode) == null)
        {
            UnityEngine.Debug.Log("Search Failed");
        }
        else
        {
            UnityEngine.Debug.Log("Path Found!");
            if (path != null)
            {
                path.Clear();
                NodeRecord backtrack = closed.Find(nr => nr.node == endNode);
                while (backtrack != null)
                {
                    path.Push(backtrack); // Push onto stack (goal to start)
                    backtrack = closed.Find(nr => nr.node == backtrack.previousNode);
                }

                foreach (NodeRecord nodeRecord in path)
                {
                    renderer = nodeRecord.node.gameObject.GetComponentInChildren<SpriteRenderer>();
                    renderer.material.color = pathColor;
                }
            }
        }

        yield return null;
    }

    public static NodeRecord SmallestElement(List<NodeRecord> records)
    {
        if (records.Count == 0) return null;

        NodeRecord lowestNodeRecord = records[0];
        float lowestCost = lowestNodeRecord.costSoFar;

        foreach (NodeRecord nodeRecord in records)
        {
            if (nodeRecord.costSoFar < lowestCost)
            {
                lowestCost = nodeRecord.costSoFar;
                lowestNodeRecord = nodeRecord;
            }
        }

        return lowestNodeRecord;
    }

    public static bool Contains(List<NodeRecord> records, Node node)
    {
        return records.Exists(nr => nr.node == node);
    }

    public static NodeRecord Find(List<NodeRecord> records, Node node)
    {
        return records.Find(nr => nr.node == node);
    }
}

/// <summary>
/// NodeRecord keeps track of individual nodes in the search.
/// </summary>
public class NodeRecord
{
    public GameObject Tile { get; set; }
    public Node node { get; set; }
    public float costSoFar { get; set; } = 0;
    public Node previousNode { get; set; }

    public void ColorTile(Color newColor)
    {
        SpriteRenderer renderer = Tile.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.material.color = newColor;
        }
    }

    public void Display(float value)
    {
        TextMesh text = Tile.GetComponent<TextMesh>();
        if (text != null)
        {
            text.text = value.ToString();
        }
    }
}
