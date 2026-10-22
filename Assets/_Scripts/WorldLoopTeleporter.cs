using UnityEngine;

public class WorldLoopTeleporter : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private Transform house;

    [Tooltip("Check this for North/South boundaries. " +
             "Uncheck for East/West boundaries.")]
    [SerializeField] private bool preserveX = true;

    private void OnTriggerEnter(Collider other)
    {
        CharacterController controller =
            other.GetComponent<CharacterController>();

        if (controller == null)
            return;

        Transform player = controller.transform;

        // Start at the destination point
        Vector3 newPosition = destination.position;

        // Preserve the player's position along the boundary.
        if (preserveX)
        {
            // North/South boundary
            newPosition.x = player.position.x;
        }
        else
        {
            // East/West boundary
            newPosition.z = player.position.z;
        }

        // Figure out which direction the house is from
        // the new location.
        Vector3 directionToHouse =
            house.position - newPosition;

        directionToHouse.y = 0f;

        Quaternion newRotation = player.rotation;

        if (directionToHouse.sqrMagnitude > 0.001f)
        {
            newRotation =
                Quaternion.LookRotation(directionToHouse);
        }

        // CharacterController doesn't like being moved
        // directly while enabled.
        controller.enabled = false;

        player.SetPositionAndRotation(
            newPosition,
            newRotation
        );

        controller.enabled = true;
    }
}