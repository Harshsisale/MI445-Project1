using UnityEngine;

/// <summary>
/// Rotates this GameObject so it always faces a target Transform.
/// Supports full 3D look-at, yaw-only (stays upright), smoothing, and a
/// rotation offset for models whose "front" isn't +Z.
/// </summary>
[DisallowMultipleComponent]
public class FaceTarget : MonoBehaviour
{
    public enum RotationMode
    {
        Full,       // Pitch + yaw: points directly at the target
        YawOnly,    // Rotates around world up only: stays upright (turrets, NPCs, billboards)
    }

    [Header("Target")]
    [Tooltip("The Transform to face. If empty, Main Camera is used when 'Use Main Camera If Empty' is on.")]
    [SerializeField] private Transform target;
    [SerializeField] private bool useMainCameraIfEmpty = false;
    [Tooltip("Local-space offset on the target (e.g. aim at the head instead of the feet).")]
    [SerializeField] private Vector3 targetOffset = Vector3.zero;

    [Header("Rotation")]
    [SerializeField] private RotationMode mode = RotationMode.Full;
    [Tooltip("Extra rotation applied after look-at. Use this if your model's front isn't +Z (e.g. (0, 180, 0) to flip).")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [Header("Smoothing")]
    [Tooltip("0 = snap instantly. Otherwise, max degrees rotated per second.")]
    [Min(0f)]
    [SerializeField] private float turnSpeed = 0f;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Start()
    {
        if (target == null && useMainCameraIfEmpty && Camera.main != null)
            target = Camera.main.transform;
    }

    // LateUpdate so we face the target's final position after it has moved this frame.
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 aimPoint = target.TransformPoint(targetOffset);
        Vector3 direction = aimPoint - transform.position;

        if (mode == RotationMode.YawOnly)
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);

        // Avoid LookRotation warnings when the target is on top of us.
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up)
                             * Quaternion.Euler(rotationOffset);

        transform.rotation = turnSpeed <= 0f
            ? desired
            : Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.TransformPoint(targetOffset));
    }
#endif
}