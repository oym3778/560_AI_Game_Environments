using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves a character according to paths found by a pathfinding algorithm.
/// </summary>
public class Character : MonoBehaviour
{
    public GameObject CurrentTile { get; set; } = null;
    public GameObject TargetTile { get; set; } = null;

    public Stack<NodeRecord> Path
    {
        get { return path; }
        set
        {
            path = value;
            Debug.Log("Path count: " + path.Count);
        }
    }

    [SerializeField] public float rotationSpeed = 3.0f;

    private Stack<NodeRecord> path = new Stack<NodeRecord>();

    [SerializeField] public float speed = 3.0f;

    [Header("Delay between steps (seconds)")]
    [SerializeField] private float waitTimeBetweenSteps = 0.5f;

    private bool isWaiting = false;

    void Update()
    {
        if (isWaiting) return;

        if (Path.Count > 0 && TargetTile == null)
        {
            Debug.Log("Popping from path, path count: " + Path.Count);
            TargetTile = Path.Pop().Tile;
            //Debug.Log("Popped Tile: " + TargetTile.name);
            Debug.Log("Popped Tile: x = " + TargetTile.transform.position.x + "Popped Tile: y = " + TargetTile.transform.position.y);
        }
        else if (TargetTile == null && Path.Count == 0)
        {
            // End of path
            return;
        }

        if (TargetTile != null)
        {
            // Move toward the target
            transform.position = Vector3.MoveTowards(transform.position, TargetTile.transform.position, speed * Time.deltaTime);

            // Get direction
            Vector2 direction = (TargetTile.transform.position - transform.position).normalized;

            // Only rotate if we're moving
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Quaternion toRotation = Quaternion.Euler(0, 0, angle);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, TargetTile.transform.position) < 0.01f)
            {
                Debug.Log("Reached TargetTile: " + TargetTile.name);
                CurrentTile = TargetTile;
                TargetTile = null;
                StartCoroutine(WaitBeforeNextStep());
            }
        }
    }

    private IEnumerator WaitBeforeNextStep()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTimeBetweenSteps);
        isWaiting = false;
    }
}
