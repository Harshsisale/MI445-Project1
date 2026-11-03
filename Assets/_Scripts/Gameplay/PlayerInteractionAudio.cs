using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractionAudio : MonoBehaviour
{
    [SerializeField] private AudioSource pickupSound;
    [SerializeField] private AudioSource diggingSound;
    [SerializeField] private AudioSource switchSound;

    public void PlayPickup()
    {
        if (pickupSound != null && pickupSound.isActiveAndEnabled && pickupSound.clip != null)
            pickupSound.PlayOneShot(pickupSound.clip);
    }

    public void PlayDig()
    {
        // Each stroke starts a fresh sound; rapid clicks don't stack digging loops.
        if (diggingSound != null && diggingSound.isActiveAndEnabled && diggingSound.clip != null)
            diggingSound.Play();
    }

    public void PlaySwitch()
    {
        if (switchSound != null && switchSound.isActiveAndEnabled && switchSound.clip != null)
            switchSound.PlayOneShot(switchSound.clip);
    }
}
