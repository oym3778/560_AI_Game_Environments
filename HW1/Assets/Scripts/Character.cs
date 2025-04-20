using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// Moves a character like Snake: either AI‑driven along a Path,
/// or player‑driven via WASD input. Colors each tile it visits.
/// </summary>
public class Character : MonoBehaviour
{

    [SerializeField] private GameObject winnerText;

    [SerializeField]
    public Character enemy = null;
    private int totalTiles;
    public int tilesColored = 0;
    // The tile the character is currently on.
    public GameObject CurrentTile { get; set; }
    private bool isMoving = false;
    // The queue of nodes to follow (AI only).
    public Stack<NodeRecord> Path { get; set; } = new Stack<NodeRecord>();
    public HashSet<GameObject> visitedTiles = new HashSet<GameObject>();

    [Header("Role Settings")]
    [Tooltip("If true, uses AI pathing; otherwise WASD control")]
    [SerializeField] public bool isAI = true; // expose in Inspector :contentReference[oaicite:0]{index=0}

    [Header("Movement Settings")]
    [SerializeField] private float speed = 3f;              // units/sec
    [SerializeField] private float waitTimeBetweenSteps = 0.5f; // delay after stepping

    [Header("Appearance")]
    [SerializeField] public Color snakeColor = Color.green; // tint color

    // Internal state
    private Direction currentDirectionEnum = Direction.Up;
    private Vector3 currentDirection = Vector3.up;
    private bool isWaiting = false;
    private GameObject TargetTile = null;

    [SerializeField]
    public bool ReachedDestination = true;



    void Start()
    {
        if (winnerText != null)
            winnerText.gameObject.SetActive(false);

        totalTiles = FindObjectsOfType<Node>().Length;

        Character[] allCharacters = FindObjectsOfType<Character>();

        foreach (var character in allCharacters)
        {
            if (character == this)
                continue;

            if (this.name.Contains("Player") && character.name.Contains("AI"))
            {
                enemy = character;
                break;
            }
            else if (this.name.Contains("AI") && character.name.Contains("Player"))
            {
                enemy = character;
                break;
            }
        }
        
        // Find initial tile under the character
        Collider2D hit = Physics2D.OverlapPoint(transform.position);
        if (hit != null && hit.CompareTag("Tile"))
        {
            CurrentTile = hit.gameObject;
            visitedTiles.Add(CurrentTile);
            ColorTile(CurrentTile, enemy);
        }

        // Default facing up
        currentDirectionEnum = Direction.Up;
        currentDirection = Vector3.up;
    }

    void Update()
    {
        if (isWaiting) return;

        if (isAI)
        {
            // AI behavior: follow path if any, else free‐move
            if ((Path.Count > 0 || TargetTile != null))
                FollowPath();
            else
            {
                //ContinueFreeMovement();
            }        
        }
        else
        {
            // Player behavior: WASD input stepping :contentReference[oaicite:1]{index=1}
            if (HandlePlayerInput())
            {
            }
            else
            {
                ContinueFreeMovement();
            }

        }
    }

    private bool HandlePlayerInput()
    {
        if (isMoving) return false;

        Direction? pressed = null;
        if (Input.GetKeyDown(KeyCode.W)) pressed = Direction.Up;
        else if (Input.GetKeyDown(KeyCode.S)) pressed = Direction.Down;
        else if (Input.GetKeyDown(KeyCode.A)) pressed = Direction.Left;
        else if (Input.GetKeyDown(KeyCode.D)) pressed = Direction.Right;

        if (pressed.HasValue)
        {
            Node nodeComp = CurrentTile.GetComponent<Node>();
            if (nodeComp.Connections.TryGetValue(pressed.Value, out GameObject nextTile))
            {
                StartCoroutine(MoveToTile(nextTile, pressed.Value));
                return true;
            }
        }
        return false;
    }

    private IEnumerator MoveToTile(GameObject nextTile, Direction direction)
    {
        isMoving = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(nextTile.transform.position.x, nextTile.transform.position.y, transform.position.z);
        float elapsed = 0f;
        float duration = Vector3.Distance(startPos, endPos) / speed;

        // Update direction and rotation
        currentDirectionEnum = direction;
        currentDirection = direction switch
        {
            Direction.Up => Vector3.up,
            Direction.Down => Vector3.down,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => currentDirection
        };
        float zRot = currentDirectionEnum == Direction.Right ? 0f :
                     currentDirectionEnum == Direction.Up ? 90f :
                     currentDirectionEnum == Direction.Left ? 180f : -90f;
        transform.rotation = Quaternion.Euler(0f, 0f, zRot);

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        CurrentTile = nextTile;
        visitedTiles.Add(CurrentTile);
        ColorTile(CurrentTile, enemy);

        yield return new WaitForSeconds(waitTimeBetweenSteps);
        isMoving = false;
    }

    /// <summary>
    /// Moves the AI character along its precomputed path.
    /// </summary>
    private void FollowPath()
    {

        if (TargetTile == null && Path.Count > 0)
        {
            ReachedDestination = false;
            // pop next node
            NodeRecord nextRecord = Path.Pop();
            TargetTile = nextRecord.Tile;

            // compute direction & rotate
            var delta = TargetTile.transform.position - CurrentTile.transform.position;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                currentDirectionEnum = (delta.x > 0) ? Direction.Right : Direction.Left;
                currentDirection = (delta.x > 0) ? Vector3.right : Vector3.left;
            }
            else
            {
                currentDirectionEnum = (delta.y > 0) ? Direction.Up : Direction.Down;
                currentDirection = (delta.y > 0) ? Vector3.up : Vector3.down;
            }
            var zRot = currentDirectionEnum == Direction.Right ? 0f :
                       currentDirectionEnum == Direction.Up ? 90f :
                       currentDirectionEnum == Direction.Left ? 180f : -90f;
            transform.rotation = Quaternion.Euler(0f, 0f, zRot);

           
        }

        if (TargetTile != null)
        {
            // move toward TargetTile
            Vector3 tp = new Vector3(TargetTile.transform.position.x,
                                     TargetTile.transform.position.y,
                                     transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, tp, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, tp) < 0.01f)
            {
                CurrentTile = TargetTile;
                visitedTiles.Add(CurrentTile);
                ColorTile(CurrentTile, enemy);
                TargetTile = null;
                StartCoroutine(WaitBeforeNextStep());
            }

            if (TargetTile == null && Path.Count == 0)
            {
                ReachedDestination = true;
                Path.Clear();
            }
        }

  
    }

    /// <summary>
    /// Continues free AI movement when no path is set.
    /// </summary>
    private void ContinueFreeMovement()
    {
        Node nodeComp = CurrentTile.GetComponent<Node>();
        if (nodeComp.Connections.TryGetValue(currentDirectionEnum, out GameObject forwardTile))
        {
            Vector3 tp = new Vector3(forwardTile.transform.position.x,
                                     forwardTile.transform.position.y,
                                     transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, tp, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, tp) < 0.01f)
            {
                CurrentTile = forwardTile;
                visitedTiles.Add(CurrentTile);
                ColorTile(CurrentTile, enemy);
                StartCoroutine(WaitBeforeNextStep());
            }
        }
    }

    /// <summary>Color the tile the snake just stepped on.</summary>
    private void ColorTile(GameObject tile, Character otherCharacter)
    {
        // Check for win condition: colored more than 80% of total tiles
        Debug.Log("totalTiles " + totalTiles);
        Debug.Log(this.name + "tilesColored " + tilesColored + " >= " + Mathf.CeilToInt(0.8f * totalTiles/2));
        if (tilesColored >= Mathf.CeilToInt(0.8f * totalTiles/2))
        {
            Debug.Log(this.name + " Won with " + tilesColored);
            SetAllTilesToGold();
        }

        SpriteRenderer currentRenderer = tile.transform.Find("Square")?.GetComponent<SpriteRenderer>();
        Color previousColor = currentRenderer != null ? currentRenderer.color : Color.clear;

        // If tile was previously colored by the other character
        if (otherCharacter != null && previousColor == otherCharacter.snakeColor)
        {
            otherCharacter.tilesColored--;
        }

        // If tile wasn't already your color, you're gaining control
        if (previousColor != snakeColor)
        {
            tilesColored++;
        }

        // Color the tile
        if (currentRenderer != null)
        {
            currentRenderer.color = snakeColor;
        }

        foreach (var rend in tile.GetComponentsInChildren<SpriteRenderer>())
        {
            if (rend.gameObject.name == "Square")
                continue;
            rend.color = snakeColor;
            rend.material.color = snakeColor;
        }

        // Record this color in the Node for resets
        Node node = tile.GetComponent<Node>();
        if (node != null)
            node.OriginalColor = snakeColor;
    }

    private void SetAllTilesToGold()
    {
        //Color victoryColor = new Color(0.6f, 0.4f, 0.8f); // Soft purple
        Color gold = new Color(0.6f, 0.4f, 0.8f); // Soft purple

        Node[] allNodes = FindObjectsOfType<Node>();
        foreach (Node node in allNodes)
        {
            // Only recolor tiles that belong to this character
            SpriteRenderer sr = node.transform.Find("Square")?.GetComponent<SpriteRenderer>();
            if (sr != null && sr.color == snakeColor)
            {
                sr.color = gold;

                foreach (var rend in node.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (rend.gameObject.name == "Square") continue;
                    rend.color = gold;
                    rend.material.color = gold;
                }

                node.OriginalColor = gold;
            }
        }
        /*
        // Display the winner's name
        if (winnerText != null)
        {
            winnerText.text = $"{gameObject.name} Wins!";
            winnerText.gameObject.SetActive(true);
        }
        */
    }

    private IEnumerator WaitBeforeNextStep()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTimeBetweenSteps);
        isWaiting = false;
    }
}