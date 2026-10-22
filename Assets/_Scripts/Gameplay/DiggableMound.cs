using UnityEngine;

public class DiggableMound : MonoBehaviour, IInteractable
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private ItemDefinition requiredShovel;
    [Tooltip("Radius of each shovel stroke in world units.")]
    [SerializeField, Min(0.01f)] private float digRadius = 0.65f;
    [Tooltip("Depth removed at the center of each stroke in world units.")]
    [SerializeField, Min(0.001f)] private float digDepth = 0.2f;
    [Tooltip("Maximum depth removed from any vertex in world units. Digging also stops at the mound's base.")]
    [SerializeField, Min(0.001f)] private float maxDigDepth = 1.5f;
    [Tooltip("How far pickups can be reached behind excavated soil, in world units. Limited to the depth dug at the aimed point. Set to zero to require a clear view.")]
    [SerializeField, Min(0f)] private float buriedPickupReach = 0.25f;
    [SerializeField] private bool resetOnEnable;

    private Mesh originalMesh;
    private Mesh originalColliderMesh;
    private Mesh runtimeMesh;
    private Vector3[] originalVertices;
    private int[] triangles;
    private Vector3[] vertices;
    private float baseHeight;

    public bool HasBeenDug { get; private set; }
    public string Prompt => "Dig mound (hold shovel)";

    private void Awake()
    {
        if (meshFilter == null)
            meshFilter = GetComponentInChildren<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null || !meshFilter.sharedMesh.isReadable)
        {
            Debug.LogError("DiggableMound needs a readable mesh. Enable Read/Write on its model importer.", this);
            enabled = false;
            return;
        }

        if (meshCollider == null)
            meshCollider = meshFilter.GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();

        originalMesh = meshFilter.sharedMesh;
        originalColliderMesh = meshCollider.sharedMesh;
        originalVertices = originalMesh.vertices;
        triangles = originalMesh.triangles;
        vertices = (Vector3[])originalVertices.Clone();

        baseHeight = float.PositiveInfinity;
        foreach (Vector3 vertex in originalVertices)
        {
            Vector3 moundPosition = transform.InverseTransformPoint(meshFilter.transform.TransformPoint(vertex));
            baseHeight = Mathf.Min(baseHeight, moundPosition.y);
        }

        // Each mound owns its mesh, so digging never changes the prefab or another instance.
        runtimeMesh = Instantiate(originalMesh);
        runtimeMesh.name = originalMesh.name + " (Dug Instance)";
        runtimeMesh.MarkDynamic();
        meshFilter.sharedMesh = runtimeMesh;
        RefreshCollider();
    }

    private void OnEnable()
    {
        if (resetOnEnable)
            ResetMound();
    }

    public void Interact(PlayerItemInteraction player)
    {
        if (player != null)
            TryDig(player, player.CurrentHit);
    }

    public float GetBuriedPickupReach(RaycastHit hit)
    {
        if (!isActiveAndEnabled || !HasBeenDug || runtimeMesh == null ||
            hit.collider != meshCollider || hit.triangleIndex < 0 || buriedPickupReach <= 0f)
            return 0f;

        int triangle = hit.triangleIndex * 3;
        if (triangle + 2 >= triangles.Length)
            return 0f;

        // Topology stays unchanged while digging, so the hit's barycentric
        // coordinates also locate the original surface before soil was removed.
        Vector3 barycentric = hit.barycentricCoordinate;
        Vector3 originalPoint = originalVertices[triangles[triangle]] * barycentric.x +
            originalVertices[triangles[triangle + 1]] * barycentric.y +
            originalVertices[triangles[triangle + 2]] * barycentric.z;
        float removedDepth = Vector3.Dot(
            meshFilter.transform.TransformPoint(originalPoint) - hit.point, transform.up);
        return Mathf.Clamp(removedDepth, 0f, buriedPickupReach);
    }

    public bool TryDig(PlayerItemInteraction player, RaycastHit hit)
    {
        if (!isActiveAndEnabled || runtimeMesh == null || player == null ||
            requiredShovel == null || player.HeldItem != requiredShovel || hit.collider != meshCollider)
            return false;

        Vector3 up = transform.up;
        float floor = Vector3.Dot(transform.TransformPoint(new Vector3(0f, baseHeight, 0f)), up);
        float radiusSquared = digRadius * digRadius;
        bool changed = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPosition = meshFilter.transform.TransformPoint(vertices[i]);
            float distanceSquared = Vector3.ProjectOnPlane(worldPosition - hit.point, up).sqrMagnitude;
            if (distanceSquared >= radiusSquared)
                continue;

            float falloff = 1f - Mathf.Sqrt(distanceSquared) / digRadius;
            falloff = falloff * falloff * (3f - 2f * falloff);
            Vector3 originalWorldPosition = meshFilter.transform.TransformPoint(originalVertices[i]);
            float removedDepth = Vector3.Dot(originalWorldPosition - worldPosition, up);
            float remainingDepth = Mathf.Max(0f, maxDigDepth - removedDepth);
            float heightAboveFloor = Mathf.Max(0f, Vector3.Dot(worldPosition, up) - floor);
            float amount = Mathf.Min(digDepth * falloff, Mathf.Min(remainingDepth, heightAboveFloor));
            if (amount <= 0.000001f)
                continue;

            vertices[i] = meshFilter.transform.InverseTransformPoint(worldPosition - up * amount);
            changed = true;
        }

        if (!changed)
            return false;

        runtimeMesh.vertices = vertices;
        runtimeMesh.RecalculateNormals();
        runtimeMesh.RecalculateTangents();
        runtimeMesh.RecalculateBounds();
        RefreshCollider();
        HasBeenDug = true;
        return true;
    }

    [ContextMenu("Reset Mound")]
    public void ResetMound()
    {
        if (runtimeMesh == null)
            return;

        System.Array.Copy(originalVertices, vertices, vertices.Length);
        runtimeMesh.vertices = vertices;
        runtimeMesh.normals = originalMesh.normals;
        runtimeMesh.tangents = originalMesh.tangents;
        runtimeMesh.bounds = originalMesh.bounds;
        RefreshCollider();
        HasBeenDug = false;
    }

    private void RefreshCollider()
    {
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = runtimeMesh;
    }

    private void OnDestroy()
    {
        if (runtimeMesh == null)
            return;

        if (meshFilter != null)
            meshFilter.sharedMesh = originalMesh;
        if (meshCollider != null)
            meshCollider.sharedMesh = originalColliderMesh;

        if (Application.isPlaying)
            Destroy(runtimeMesh);
        else
            DestroyImmediate(runtimeMesh);
    }
}
