using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves a character like Snake: either AI‑driven along a Path,
/// or player‑driven via WASD input. Colors each tile it visits.
/// </summary>
public class Character : MonoBehaviour
{
    // The tile the character is currently on.
    public GameObject CurrentTile { get; set; }
    private bool isMoving = false;
    // The queue of nodes to follow (AI only).
    public Stack<NodeRecord> Path { get; set; } = new Stack<NodeRecord>();

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

    void Start()
    {
        // Find initial tile under the character
        Collider2D hit = Physics2D.OverlapPoint(transform.position);
        if (hit != null && hit.CompareTag("Tile"))
        {
            CurrentTile = hit.gameObject;
            ColorTile(CurrentTile);
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
            if (Path.Count > 0 || TargetTile != null)
                FollowPath();
            else
                ContinueFreeMovement();
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
        ColorTile(CurrentTile);

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
                ColorTile(CurrentTile);
                TargetTile = null;
                StartCoroutine(WaitBeforeNextStep());
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
                ColorTile(CurrentTile);
                StartCoroutine(WaitBeforeNextStep());
            }
        }
    }

    /// <summary>Color the tile the snake just stepped on.</summary>
    private void ColorTile(GameObject tile)
    {
        var rend = tile.GetComponentInChildren<SpriteRenderer>();
        if (rend != null) rend.material.color = snakeColor;
        var node = tile.GetComponent<Node>();
        if (node != null) node.OriginalColor = snakeColor;
    }

    private IEnumerator WaitBeforeNextStep()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTimeBetweenSteps);
        isWaiting = false;
    }
}