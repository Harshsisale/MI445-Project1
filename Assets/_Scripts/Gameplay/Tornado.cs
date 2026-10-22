using UnityEngine;
using UnityEngine.SceneManagement;

public class Tornado : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to auto-find the object tagged 'Player'.")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [Tooltip("How fast the tornado moves toward the player (units per second).")]
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("The tornado stops approaching once it's this close to the player.")]
    [SerializeField] private float stopDistance = 0f;

    [Tooltip("If true, the tornado only moves on the XZ plane and keeps its own height.")]
    [SerializeField] private bool stayOnGround = true;

    [Header("Visuals")]
    [Tooltip("Spin speed around the world Y axis in degrees per second. Set to 0 to freeze rotation completely.")]
    [SerializeField] private float spinSpeed = 180f;

    private Rigidbody rb;

    // Rotation the tornado started with. X/Z tilt is locked to this.
    private Quaternion baseRotation;

    // Accumulated spin around world Y, applied on top of baseRotation.
    private float yaw;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Physics (collisions, forces) can never rotate the tornado.
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        baseRotation = transform.rotation;
        yaw = 0f;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("Tornado: No player assigned and no object tagged 'Player' found.", this);
            }
        }
    }

    // Used when there is no Rigidbody: drive the Transform directly.
    void Update()
    {
        if (rb != null) return;

        Vector3 nextPos = ComputeNextPosition(transform.position, Time.deltaTime);
        Quaternion nextRot = ComputeNextRotation(Time.deltaTime);
        transform.SetPositionAndRotation(nextPos, nextRot);
    }

    // Used when there is a Rigidbody: drive it through physics so it doesn't fight the simulation.
    void FixedUpdate()
    {
        if (rb == null) return;

        rb.MovePosition(ComputeNextPosition(rb.position, Time.fixedDeltaTime));
        rb.MoveRotation(ComputeNextRotation(Time.fixedDeltaTime));
    }

    private Vector3 ComputeNextPosition(Vector3 currentPos, float dt)
    {
        if (player == null) return currentPos;

        Vector3 targetPos = player.position;
        if (stayOnGround)
        {
            targetPos.y = currentPos.y;
        }

        float distance = Vector3.Distance(currentPos, targetPos);
        if (distance <= stopDistance) return currentPos;

        return Vector3.MoveTowards(currentPos, targetPos, moveSpeed * dt);
    }

    private Quaternion ComputeNextRotation(float dt)
    {
        yaw = Mathf.Repeat(yaw + spinSpeed * dt, 360f);

        // Only world-Y spin is ever applied; X/Z tilt stays locked to the starting rotation.
        return Quaternion.AngleAxis(yaw, Vector3.up) * baseRotation;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}