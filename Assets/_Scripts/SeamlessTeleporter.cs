using UnityEngine;

public class SeamlessTeleporter : MonoBehaviour
{
    [SerializeField] private Transform destination;

    private Transform player;
    private CharacterController controller;

    private float previousSide;
    private bool playerInside;

    private void OnTriggerEnter(Collider other)
    {
        CharacterController foundController =
            other.GetComponent<CharacterController>();

        if (foundController == null)
            return;

        player = foundController.transform;
        controller = foundController;

        previousSide = GetSide(player.position);
        playerInside = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!playerInside || player == null)
            return;

        float currentSide = GetSide(player.position);

        // Check if the player has crossed the plane of the teleporter
        bool crossedPlane =
            (previousSide > 0f && currentSide <= 0f) ||
            (previousSide < 0f && currentSide >= 0f);

        // If the player has crossed the plane, teleport them to the destination
        if (crossedPlane)
        {
            Teleport();
            return;
        }

        // Update the previous side for the next frame
        previousSide = currentSide;
    }

    private void OnTriggerExit(Collider other)
    {
        CharacterController foundController =
            other.GetComponent<CharacterController>();

        if (foundController == null)
            return;

        if (foundController == controller)
        {
            playerInside = false;
            player = null;
            controller = null;
        }
    }

    private float GetSide(Vector3 position)
    {
        Vector3 toPlayer = position - transform.position;

        return Vector3.Dot(transform.forward, toPlayer);
    }

    private void Teleport()
    {
        Vector3 localPosition =
            transform.InverseTransformPoint(player.position);

        Quaternion localRotation =
            Quaternion.Inverse(transform.rotation) *
            player.rotation;

        Vector3 destinationPosition =
            destination.TransformPoint(localPosition);

        Quaternion destinationRotation =
            destination.rotation * localRotation;

        controller.enabled = false;

        player.SetPositionAndRotation(
            destinationPosition,
            destinationRotation
        );

        controller.enabled = true;

        playerInside = false;
        player = null;
        controller = null;
    }
}