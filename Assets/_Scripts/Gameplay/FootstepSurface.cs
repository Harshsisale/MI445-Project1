using UnityEngine;

/// <summary>
/// Labels a floor or a parent containing indoor geometry. Unmarked ground is outdoors.
/// A marker on a child overrides one on its parent.
/// </summary>
[DisallowMultipleComponent]
public class FootstepSurface : MonoBehaviour
{
    [SerializeField] private bool indoors = true;

    public bool Indoors => indoors;
}
