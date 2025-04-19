using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves a character like Snake: follows a precomputed path when available,
/// otherwise continues moving in its current facing direction,
/// and colors each tile it visits.
/// </summary>
public class Character : MonoBehaviour
{
    // The tile the character is currently on.
    public GameObject CurrentTile { get; set; }

    // The stack of path nodes to follow (if any).
    public Stack<NodeRecord> Path { get; set; } = new Stack<NodeRecord>();

    [Header("Movement Settings")]
    [SerializeField] private float speed = 3f;              // units per second
    [SerializeField] private float waitTimeBetweenSteps = 0.5f; // delay after reaching a tile

    [Header("Snake Appearance")]
    [SerializeField] private Color snakeColor = Color.green; // set via interface

    // Internal state
    private Direction currentDirectionEnum = Direction.Up;   // initial facing
    private Vector3 currentDirection = Vector3.up;          // movement vector
    private bool isWaiting = false;

    // Target tile when following a path
    private GameObject TargetTile = null;

    void Start()
    {
        // Initialize CurrentTile by detecting the tile under the character at start
        Collider2D hit = Physics2D.OverlapPoint(transform.position);
        if (hit != null && hit.CompareTag("Tile"))
        {
            CurrentTile = hit.gameObject;
            ColorTile(CurrentTile);
        }

        // Ensure initial direction faces up
        currentDirectionEnum = Direction.Up;
        currentDirection = Vector3.up;
    }

    void Update()
    {
        if (isWaiting) return;

        // If we have a path, follow it
        if (Path.Count > 0 || TargetTile != null)
        {
            FollowPath();
        }
        else
        {
            // No path: free movement in current direction (like Snake)
            ContinueFreeMovement();
        }
    }

    /// <summary>
    /// Moves the character along the precomputed Path.
    /// </summary>
    private void FollowPath()
    {
        // If no target yet, pop next node
        if (TargetTile == null && Path.Count > 0)
        {
            NodeRecord nextRecord = Path.Pop();
            TargetTile = nextRecord.Tile;

            // Determine direction from CurrentTile to TargetTile
            Vector3 delta = TargetTile.transform.position - CurrentTile.transform.position;
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

            // Rotate sprite to face new direction
            float zRot = currentDirectionEnum == Direction.Right ? 0f :
                         currentDirectionEnum == Direction.Up ? 90f :
                         currentDirectionEnum == Direction.Left ? 180f :
                                                                 -90f;
            transform.rotation = Quaternion.Euler(0f, 0f, zRot);
        }

        // Move toward the TargetTile
        if (TargetTile != null)
        {
            Vector3 targetPos = new Vector3(TargetTile.transform.position.x,
                                            TargetTile.transform.position.y,
                                            transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            // Upon arrival, update CurrentTile and clear TargetTile
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                CurrentTile = TargetTile;
                ColorTile(CurrentTile);
                TargetTile = null;
                StartCoroutine(WaitBeforeNextStep());
            }
        }
    }

    /// <summary>
    /// Continues free movement in the current direction.
    /// </summary>
    private void ContinueFreeMovement()
    {
        // Check that there is a tile ahead before moving
        Node nodeComp = CurrentTile.GetComponent<Node>();
        if (nodeComp.Connections.TryGetValue(currentDirectionEnum, out GameObject forwardTile))
        {
            Vector3 targetPos = new Vector3(forwardTile.transform.position.x,
                                            forwardTile.transform.position.y,
                                            transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            // Upon arrival, update CurrentTile and pause
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                CurrentTile = forwardTile;
                ColorTile(CurrentTile);
                StartCoroutine(WaitBeforeNextStep());
            }
        }
        else
        {
            // No tile ahead: halt movement (snake would die or reset here)
        }
    }

    /// <summary>
    /// Colors the tile to the snake's color.
    /// </summary>
    private void ColorTile(GameObject tile)
    {
        SpriteRenderer rend = tile.GetComponentInChildren<SpriteRenderer>();
        if (rend != null)
            rend.material.color = snakeColor;

        // Also update the Node's OriginalColor if needed
        Node node = tile.GetComponent<Node>();
        if (node != null)
            node.OriginalColor = snakeColor;
    }

    private IEnumerator WaitBeforeNextStep()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTimeBetweenSteps);
        isWaiting = false;
    }
}