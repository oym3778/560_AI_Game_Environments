using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// Provides an interface that allows the user to select tiles to perform pathfinding between.
/// Then moves a character between those tiles.
/// </summary>
public class CharacterInterface : MonoBehaviour
{
    // User selection for pathfinding start/end
    private GameObject from = null;
    private GameObject to = null;

    // Flag to prevent concurrent searches
    private bool searching = false;

    // Reference to Graph
    private Graph graph;

    // Prefabs for AI and Player
    [Header("Prefabs")]
    public GameObject characterPrefab;  // AI snake prefab
    public GameObject playerPrefab;     // Player snake prefab (can be same or different)

    // Instances
    private GameObject aiGO;
    private Character aiCharacter;
    private GameObject playerGO;
    private Character playerCharacter;

    [Header("Visualization Settings")]
    public float waitTime = 1f;
    public SearchType searchType = SearchType.Dijkstra;
    public HeuristicType heuristicType = HeuristicType.Uniform;
    public bool colorTiles = true;
    public bool displayCosts = false;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize graph
        GameObject graphGO = GameObject.Find("Graph");
        graph = graphGO.GetComponent<Graph>();
        graph.makeGraph();

        // Compute graph dimensions
        int width = 10 * graph.scale;
        int height = 16 * graph.scale;

        // --- Spawn AI at top-left (0,0) ---
        GameObject aiStartTile = graph.getTile(0, 0);
        aiGO = Instantiate(characterPrefab, aiStartTile.transform.position, Quaternion.identity);
        SetupCharacter(aiGO, Color.red, aiStartTile);
        aiCharacter = aiGO.GetComponent<Character>();
        aiCharacter.ReachedDestination = true;
        // --- Spawn Player at bottom-right (width-1, height-1) ---
        GameObject playerStartTile = graph.getTile(width - 1, height - 1);
        playerGO = Instantiate(playerPrefab != null ? playerPrefab : characterPrefab,
                               playerStartTile.transform.position,
                               Quaternion.identity);
        SetupCharacter(playerGO, Color.blue, playerStartTile);
        playerCharacter = playerGO.GetComponent<Character>();

        // Log spawning
        Debug.Log($"AI spawned on tile: {aiStartTile.name}");
        Debug.Log($"Player spawned on tile: {playerStartTile.name}");
    }

    /// <summary>
    /// Common initialization for a snake character.
    /// </summary>
    private void SetupCharacter(GameObject go, Color tint, GameObject startTile)
    {
        // Color the snake
        foreach (var rend in go.GetComponentsInChildren<SpriteRenderer>())
        {
            rend.material.color = tint;
        }

        // Scale to fit tile
        float invScale = 1f / graph.scale;
        go.transform.localScale = new Vector3(invScale * 0.6f, invScale * 0.6f, 1f);

        // Adjust spawn offset to be more centered
        go.transform.position = new Vector3(
            startTile.transform.position.x + (invScale * 0.5f),
            startTile.transform.position.y - (invScale * 0.5f),
            go.transform.position.z);

        // Set the snake's current tile reference
        go.GetComponent<Character>().CurrentTile = startTile;
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (Input.GetMouseButtonDown(0) && !searching)
        {
            StartCoroutine(HandleInput());
        }
        */
        
        if (!searching && aiCharacter.ReachedDestination)
        {
            GameObject targetTile = FindNearestUncoloredTile(aiCharacter.CurrentTile, aiCharacter.snakeColor);
            //GameObject targetTile = GetRandomTile();
            //Debug.Log("A PATH  WAS FOUND");
            if (targetTile != null)
            {
                Debug.Log("A PATH  WAS FOUND");
                from = aiCharacter.CurrentTile;
                to = targetTile;
                searching = true;
                StartCoroutine(PerformSearch(from, to));
            }
        }
        
    }
    private GameObject GetRandomTile()
    {
        int width = 10 * graph.scale;
        int height = 16 * graph.scale;

        GameObject tile = null;

        while (tile == null)
        {
            int randomX = Random.Range(0, width);
            int randomY = Random.Range(0, height);
            tile = graph.getTile(randomX, randomY);
        }

        return tile;
    }
    private GameObject FindNearestUncoloredTile(GameObject startTile, Color aiColor)
    {
        int width = 10 * graph.scale;
        int height = 16 * graph.scale;

        Queue<GameObject> queue = new Queue<GameObject>();
        HashSet<GameObject> visited = new HashSet<GameObject>();

        queue.Enqueue(startTile);
        visited.Add(startTile);

        while (queue.Count > 0)
        {
            GameObject current = queue.Dequeue();

            Transform squareTf = current.transform.Find("Square");
            SpriteRenderer squareRend = squareTf?.GetComponent<SpriteRenderer>();

            if (squareRend != null && !ColorApproximatelyEqual(squareRend.color, aiColor))
            {
                return current;
            }

            // Add adjacent tiles (4-way)
            Vector2Int coord = graph.getTileCoords(current);
            Vector2Int[] directions = new Vector2Int[]
            {
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(1, 0),  // Right
            new Vector2Int(-1, 0)  // Left
            };

            foreach (var dir in directions)
            {
                int newX = coord.x + dir.x;
                int newY = coord.y + dir.y;

                if (newX < 0 || newY < 0 || newX >= width || newY >= height)
                    continue;

                GameObject neighbor = graph.getTile(newX, newY);
                if (neighbor != null && !visited.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        // No uncolored tile found
        return null;

    }
    private bool ColorApproximatelyEqual(Color a, Color b, float tolerance = 0.05f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance &&
               Mathf.Abs(a.a - b.a) < tolerance;
    }

    // A coroutine that handles input from the user and starts search.
    private IEnumerator HandleInput ()
    {
        // If the mouse has been clicked and there isn't a current search...
        if (Input.GetMouseButtonDown(0) && !searching)
        {
            // Grab the position that was clicked by the mouse.
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

            // Use a raycast to determine whether a tile was clicked.
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            // If a tile with a collider was clicked...
            if (hit.collider != null)
            {
                // Set the from tile to the character's current tile.
                from = aiCharacter.CurrentTile;

                // Grab the renderer of the clicked tile.
                SpriteRenderer renderer = hit.collider.gameObject.GetComponentInChildren<SpriteRenderer>();

                // Turn the tile color to magenta to visualize the selection.
                renderer.material.color = Color.magenta;

                //  If the to tile is null and the current is different than the stored from tile...
                if (to == null && hit.collider.gameObject != from && !searching)
                {
                    // Set the to game object to the current tile.
                    to = hit.collider.gameObject;
                    Debug.Log("TO TILE IS: " + to);
                    // Store that we are currently searching.
                    searching = true;

                    // Create a stack to store the path found by the algorithm.
                    Stack<NodeRecord> path = new Stack<NodeRecord>();

                    // Start a new search coroutine based on the stored search type and heuristic.
                    // Also, print a line to the log stating what type of search has been started.
                    if (searchType == SearchType.Dijkstra)
                    {
                        Debug.Log("Dijkstra");
                        yield return StartCoroutine(Dijkstra.search(from, to, waitTime, colorTiles, displayCosts, path));
                    }
                    else if (searchType == SearchType.AStar)
                    {
                        if (heuristicType == HeuristicType.Uniform)
                        {
                            Debug.Log("A* Uniform");
                            yield return StartCoroutine(AStar.search(from, to, AStar.Uniform, waitTime, colorTiles, displayCosts, path));
                        }
                        else if (heuristicType == HeuristicType.Manhattan)
                        {
                            Debug.Log("A* Manhattan");
                            yield return StartCoroutine(AStar.search(from, to, AStar.Manhattan, waitTime, colorTiles, displayCosts, path));
                        }
                        else if (heuristicType == HeuristicType.CrossProduct)
                        {
                            Debug.Log("A* Cross Product");
                            yield return StartCoroutine(AStar.search(from, to, AStar.CrossProduct, waitTime, colorTiles, displayCosts, path));
                        }
                    }

                    // Pass the final path to the character.
                    //character.Path = new Stack<NodeRecord>(path);
                    aiCharacter.Path = path;
                    // Search is now over.
                    searching = false;
                }

                // If both tiles are filled, this is a post-search click.
                // Reset the graph and prepare for a new search.
                else
                {
                    // Reset the graph color.
                    graph.resetColor();

                    // Reset the tile variables.
                    from = null;
                    to = null;
                }
            }
        }

        yield return null;
    }



    private IEnumerator PerformSearch(GameObject from, GameObject to)
    {
        Stack<NodeRecord> path = new Stack<NodeRecord>();

        if (searchType == SearchType.Dijkstra)
        {
            yield return StartCoroutine(Dijkstra.search(from, to, waitTime, colorTiles, displayCosts, path));
        }
        else if (searchType == SearchType.AStar)
        {
            if (heuristicType == HeuristicType.Uniform)
            {
                yield return StartCoroutine(AStar.search(from, to, AStar.Uniform, waitTime, colorTiles, displayCosts, path));
            }
            else if (heuristicType == HeuristicType.Manhattan)
            {
                yield return StartCoroutine(AStar.search(from, to, AStar.Manhattan, waitTime, colorTiles, displayCosts, path));
            }
            else if (heuristicType == HeuristicType.CrossProduct)
            {
                yield return StartCoroutine(AStar.search(from, to, AStar.CrossProduct, waitTime, colorTiles, displayCosts, path));
            }
        }

        aiCharacter.Path = path;
        Debug.Log($"Assigned path with {path.Count} nodes.");
        searching = false;
    }

}

