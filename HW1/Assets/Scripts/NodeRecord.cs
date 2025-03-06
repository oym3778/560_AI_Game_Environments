using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// A class for recording search statistics.
/// </summary>
public class NodeRecord
{
    // The tile game object.
    public GameObject Tile { get; set; } = null;
    // Set the other class properties here.
    public Node node { get; set; } = null;
    public NodeRecord fromNode { get; set; } = null;
    public float costSoFar { get; set; } = 0;
    public float cost { get; set; } = 1;
    private Dictionary<Direction, NodeRecord> connections = new Dictionary<Direction, NodeRecord>();

    public Dictionary<Direction, GameObject> getConnections()
    {
        return node.Connections;
    }

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