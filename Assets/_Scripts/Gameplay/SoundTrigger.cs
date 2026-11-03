using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody), typeof(AudioSource))]
public class SoundTrigger : MonoBehaviour
{
    [Tooltip("When enabled, only the first player entry plays the sound.")]
    [SerializeField] private bool playOnce;
    [Tooltip("Minimum seconds between sounds when the player exits and re-enters.")]
    [SerializeField, Min(0f)] private float cooldown = 0.5f;

    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();
    private AudioSource sound;
    private bool hasPlayed;
    private float nextPlayTime;

    private void Awake()
    {
        sound = GetComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.loop = false;
        ConfigureTrigger();
    }

    private void Reset()
    {
        ConfigureTrigger();
        AudioSource source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
    }

    private void ConfigureTrigger()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled)
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null)
            return;

        playerColliders.RemoveWhere(collider => collider == null || !collider.enabled ||
            !collider.gameObject.activeInHierarchy);
        if (!playerColliders.Add(other) || playerColliders.Count > 1 ||
            (playOnce && hasPlayed) || Time.time < nextPlayTime)
            return;

        if (sound.isActiveAndEnabled && sound.clip != null)
            sound.Play();
        hasPlayed = true;
        nextPlayTime = Time.time + cooldown;
        GetComponent<ThresholdScareCutscene>()?.Play(player);
    }

    private void OnTriggerExit(Collider other)
    {
        playerColliders.Remove(other);
    }

    private void OnDisable()
    {
        playerColliders.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
