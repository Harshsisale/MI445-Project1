using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryDropper : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform dropOrigin;
    [SerializeField] private WorldItemPickup fallbackPrefab;
    [SerializeField, Min(0.1f)] private float dropDistance = 1.5f;
    [SerializeField, Min(0.01f)] private float dropClearance = 0.05f;

    private const int DirectionCount = 16;
    private const int RotationCount = 4;
    private const int DistanceSteps = 3;
    private const int QueryCapacity = 64;

    // Bounds include the prefab and item's scale, measured at root identity.
    // Rotating this box gives a conservative oriented volume without recooking
    // meshes or moving a physics object for every candidate.
    private readonly Dictionary<(GameObject prefab, Vector3 scale), Bounds> prefabBounds = new();
    private readonly List<Collider> colliders = new();
    private readonly List<Renderer> renderers = new();
    private readonly Collider[] overlapResults = new Collider[QueryCapacity];
    private readonly RaycastHit[] castResults = new RaycastHit[QueryCapacity];

    public bool TryDrop(int slotIndex)
    {
        if (inventory == null || dropOrigin == null ||
            slotIndex < 0 || slotIndex >= inventory.Slots.Count)
            return false;

        InventorySlot slot = inventory.Slots[slotIndex];
        ItemDefinition item = slot.item;
        if (item == null || slot.quantity <= 0)
            return false;

        GameObject prefab = item.worldPrefab != null
            ? item.worldPrefab
            : fallbackPrefab != null ? fallbackPrefab.gameObject : null;
        if (prefab == null)
            return false;

        Vector3 modelScale = item.worldModelScale;
        if (!IsValidScale(modelScale.x) || !IsValidScale(modelScale.y) ||
            !IsValidScale(modelScale.z))
            return false;

        Vector3 instanceScale = Vector3.Scale(prefab.transform.localScale, modelScale);
        var boundsKey = (prefab, modelScale);
        GameObject spawned = null;
        Physics.SyncTransforms();
        if (!prefabBounds.TryGetValue(boundsKey, out Bounds bounds))
        {
            // This first instance is measured and disabled within the same
            // call, before any rendering or physics step can occur.
            spawned = Instantiate(prefab, dropOrigin.position, Quaternion.identity);
            spawned.transform.localScale = instanceScale;
            spawned.SetActive(true);
            Physics.SyncTransforms();
            bool hasBounds = TryGetBounds(spawned, out bounds);
            bounds.center -= spawned.transform.position;
            spawned.SetActive(false);

            if (!hasBounds)
            {
                Destroy(spawned);
                return false;
            }

            prefabBounds.Add(boundsKey, bounds);
        }

        if (!TryFindDropPose(bounds, prefab.transform.rotation, item.dropHorizontally,
                out Vector3 position, out Quaternion rotation))
        {
            if (spawned != null)
                Destroy(spawned);
            return false;
        }

        if (spawned == null)
            spawned = Instantiate(prefab, position, rotation);
        else
            spawned.transform.SetPositionAndRotation(position, rotation);

        spawned.transform.localScale = instanceScale;
        WorldItemPickup pickup = PreparePickup(spawned, bounds);
        pickup.Initialize(item, 1);

        if (!inventory.RemoveFromSlot(slotIndex, 1))
        {
            spawned.SetActive(false);
            Destroy(spawned);
            return false;
        }

        spawned.SetActive(true);
        return true;
    }

    private bool TryFindDropPose(Bounds bounds, Quaternion authoredRotation, bool dropHorizontally,
        out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;
        float clearance = Mathf.Max(0.01f, dropClearance);
        Vector3 extents = bounds.extents + Vector3.one * clearance;
        Vector3 origin = dropOrigin.position;

        // Check once: rays alone cannot detect a wall containing their origin.
        int overlaps = Physics.OverlapSphereNonAlloc(origin, 0.01f, overlapResults,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
        if (overlaps == QueryCapacity)
            return false;
        for (int i = 0; i < overlaps; i++)
            if (!IsPlayerCollider(overlapResults[i]))
                return false;

        Bounds playerBounds = new Bounds(origin, Vector3.zero);
        inventory.GetComponentsInChildren(false, colliders);
        bool hasPlayerBounds = false;
        foreach (Collider collider in colliders)
        {
            if (!collider.enabled || collider.isTrigger)
                continue;
            if (hasPlayerBounds)
                playerBounds.Encapsulate(collider.bounds);
            else
                playerBounds = collider.bounds;
            hasPlayerBounds = true;
        }

        Vector3 forward = Vector3.ProjectOnPlane(dropOrigin.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 longAxis = bounds.size.x >= bounds.size.y && bounds.size.x >= bounds.size.z
            ? Vector3.right : bounds.size.y >= bounds.size.z ? Vector3.up : Vector3.forward;
        Quaternion layFlat = Quaternion.FromToRotation(longAxis, Vector3.forward);

        // Fixed search limit: 16 directions, four poses, three distances, and
        // at most two heights. Most drops finish at the first clear candidate.
        for (int directionIndex = 0; directionIndex < DirectionCount; directionIndex++)
        {
            int angleStep = (directionIndex + 1) / 2;
            float angle = angleStep * (360f / DirectionCount) *
                (directionIndex % 2 == 0 ? -1f : 1f);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;

            int rotationCount = dropHorizontally ? 2 : RotationCount;
            for (int rotationIndex = 0; rotationIndex < rotationCount; rotationIndex++)
            {
                Quaternion pose;
                if (dropHorizontally)
                {
                    Vector3 flatDirection = rotationIndex == 0
                        ? direction : Vector3.Cross(Vector3.up, direction);
                    pose = Quaternion.LookRotation(flatDirection) * layFlat;
                }
                else
                {
                    switch (rotationIndex)
                    {
                        case 0: pose = authoredRotation; break;
                        case 1: pose = Quaternion.LookRotation(direction) * layFlat; break;
                        case 2: pose = Quaternion.LookRotation(Vector3.Cross(Vector3.up, direction)) * layFlat; break;
                        default: pose = Quaternion.FromToRotation(longAxis, Vector3.up); break;
                    }
                }

                // Move the bounds center far enough to clear both shapes.
                // An offset model pivot never increases the required distance.
                Vector3 absoluteDirection = Abs(direction);
                float playerRadius = Vector3.Dot(playerBounds.extents, absoluteDirection);
                float itemRadius = ProjectedRadius(extents, pose, direction);
                float minimumDistance = Mathf.Max(clearance,
                    Vector3.Dot(playerBounds.center - origin, direction) +
                    playerRadius + itemRadius + clearance);
                float preferredDistance = Mathf.Max(dropDistance, minimumDistance);
                float floorHeight = playerBounds.min.y +
                    ProjectedRadius(extents, pose, Vector3.up) + clearance;
                float preferredHeight = Mathf.Max(origin.y, floorHeight);
                int heightCount = preferredHeight - floorHeight > clearance ? 2 : 1;

                for (int distanceStep = 0; distanceStep < DistanceSteps; distanceStep++)
                {
                    float distance = distanceStep == 0 ? preferredDistance :
                        distanceStep == 1 ? minimumDistance :
                        preferredDistance + Mathf.Max(0.1f, dropDistance);
                    if (distanceStep == 1 && preferredDistance - minimumDistance < clearance)
                        continue;

                    for (int heightStep = 0; heightStep < heightCount; heightStep++)
                    {
                        Vector3 center = origin + direction * distance;
                        center.y = heightStep == 0 ? preferredHeight : floorHeight;
                        if (Physics.CheckBox(center, extents, pose,
                                Physics.AllLayers, QueryTriggerInteraction.Ignore) ||
                            !HasClearPath(origin, center))
                            continue;

                        // Sweep the volume up to the nearest placement that
                        // clears the player. The ray checks the remaining gap.
                        float sweepDistance = distance - minimumDistance;
                        if (sweepDistance > clearance && !HasClearSweep(center,
                                extents, pose, -direction, sweepDistance))
                            continue;

                        position = center - pose * bounds.center;
                        rotation = pose;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool HasClearPath(Vector3 origin, Vector3 target)
    {
        Vector3 travel = target - origin;
        int hits = Physics.RaycastNonAlloc(origin, travel.normalized, castResults,
            travel.magnitude, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        return OnlyPlayerHits(hits);
    }

    private bool HasClearSweep(Vector3 center, Vector3 extents, Quaternion rotation,
        Vector3 direction, float distance)
    {
        int hits = Physics.BoxCastNonAlloc(center, extents, direction, castResults,
            rotation, distance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        return OnlyPlayerHits(hits);
    }

    private bool OnlyPlayerHits(int count)
    {
        // A full buffer may hide an obstruction. Refuse instead of allocating.
        if (count == QueryCapacity)
            return false;
        for (int i = 0; i < count; i++)
            if (!IsPlayerCollider(castResults[i].collider))
                return false;
        return true;
    }

    private bool IsPlayerCollider(Collider collider)
    {
        return collider.transform.IsChildOf(inventory.transform);
    }

    private static Vector3 Abs(Vector3 vector)
    {
        return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
    }

    private static bool IsValidScale(float scale)
    {
        return scale > 0f && !float.IsNaN(scale) && !float.IsInfinity(scale);
    }

    private static float ProjectedRadius(Vector3 extents, Quaternion rotation, Vector3 axis)
    {
        return Vector3.Dot(extents, Abs(Quaternion.Inverse(rotation) * axis));
    }

    private bool TryGetBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        instance.GetComponentsInChildren(false, colliders);
        foreach (Collider collider in colliders)
        {
            if (!collider.enabled)
                continue;
            if (found)
                bounds.Encapsulate(collider.bounds);
            else
                bounds = collider.bounds;
            found = true;
        }

        instance.GetComponentsInChildren(false, renderers);
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
                continue;
            if (found)
                bounds.Encapsulate(renderer.bounds);
            else
                bounds = renderer.bounds;
            found = true;
        }

        return found && bounds.size.sqrMagnitude > 0f;
    }

    private static WorldItemPickup PreparePickup(GameObject instance, Bounds bounds)
    {
        WorldItemPickup pickup = instance.GetComponent<WorldItemPickup>();
        if (pickup != null)
            return pickup;

        // Imported models can be used directly as worldPrefab. Supply a fitted
        // root collider before adding the pickup's required Collider component.
        if (instance.GetComponent<Collider>() == null)
        {
            BoxCollider box = instance.AddComponent<BoxCollider>();
            Vector3 scale = instance.transform.localScale;
            box.center = new Vector3(bounds.center.x / scale.x,
                bounds.center.y / scale.y, bounds.center.z / scale.z);
            box.size = new Vector3(bounds.size.x / Mathf.Abs(scale.x),
                bounds.size.y / Mathf.Abs(scale.y), bounds.size.z / Mathf.Abs(scale.z));
        }

        if (instance.GetComponent<Rigidbody>() == null)
        {
            Rigidbody body = instance.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        return instance.AddComponent<WorldItemPickup>();
    }
}
