using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// First-person body cam effect. Drop it on the Main Camera.
///
/// Adds: chest-mount offset, wide FOV, look lag with overshoot, turn/strafe roll,
/// walk bob with footstep kicks, landing impacts, handheld sway/jitter, and trauma shake.
///
/// Works alongside any existing player controller: the effect is layered on top in LateUpdate
/// and stripped back off at the end of the frame, so your controller (and raycasts that use
/// the camera transform) always see the clean, un-shaken pose.
/// </summary>
[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class BodyCamera : MonoBehaviour
{
    [Header("Master")]
    [Tooltip("Scales every motion effect. 0 = off, 1 = default. Hook this to a motion-sickness slider.")]
    [Range(0f, 2f)] public float intensity = 1f;

    [Header("Mount")]
    [Tooltip("Offset from the camera's normal spot, in the player's facing space (x = right, y = up, z = forward). " +
             "Default sits low and slightly right, like a chest-mounted cam.")]
    [SerializeField] private Vector3 mountOffset = new Vector3(0.06f, -0.25f, 0.1f);

    [Header("Lens")]
    [Tooltip("Turn off if another script controls the camera FOV.")]
    [SerializeField] private bool controlFOV = true;
    [SerializeField] private float baseFOV = 92f;
    [SerializeField] private float sprintFOVBoost = 5f;
    [Tooltip("Speed (m/s) at which the full sprint FOV boost is reached.")]
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float fovSmoothing = 5f;

    [Header("Built-in Mouse Look (optional)")]
    [Tooltip("Leave OFF if your player controller already rotates the camera.")]
    [SerializeField] private bool useBuiltInMouseLook = false;
    [Tooltip("Yaw is applied here (usually the player root). If empty, yaw is applied to the camera.")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float pitchLimit = 85f;
    [SerializeField] private bool lockCursor = true;

    [Header("Movement Source")]
    [Tooltip("Optional, but makes grounded/landing detection reliable. Auto-found in parents.")]
    [SerializeField] private CharacterController characterController;
    [Tooltip("Speed (m/s) at which the walk bob plays at full strength.")]
    [SerializeField] private float walkSpeed = 4f;

    [Header("Look Lag (chest-mount inertia)")]
    [Tooltip("How much the view trails behind your look input.")]
    [SerializeField, Range(0f, 1f)] private float lagAmount = 0.2f;
    [SerializeField] private float lagStiffness = 80f;
    [Tooltip("Lower = more overshoot/wobble when you stop turning.")]
    [SerializeField] private float lagDamping = 10f;
    [SerializeField] private float maxLagAngle = 7f;
    [Tooltip("Roll (deg) per deg/s of turning. Negate to flip direction.")]
    [SerializeField] private float turnRoll = 0.012f;
    [SerializeField] private float maxTurnRoll = 5f;
    [Tooltip("Roll (deg) per m/s of strafing. Negate to flip direction.")]
    [SerializeField] private float strafeRoll = 0.6f;
    [SerializeField] private float rollSmoothing = 6f;

    [Header("Walk Bob")]
    [Tooltip("Footsteps per second at Walk Speed. Scales up when moving faster.")]
    [SerializeField] private float stepsPerSecond = 1.9f;
    [SerializeField] private float bobVertical = 0.03f;
    [SerializeField] private float bobSideways = 0.025f;
    [SerializeField] private float bobRoll = 1.4f;
    [SerializeField] private float bobPitch = 0.7f;
    [Tooltip("Downward pitch jolt on each footstep (deg).")]
    [SerializeField] private float stepKick = 0.9f;
    [Tooltip("Downward position jolt on each footstep (m).")]
    [SerializeField] private float stepDip = 0.012f;

    [Header("Landing")]
    [Tooltip("Fall speed (m/s) needed to trigger a landing impact.")]
    [SerializeField] private float landingMinSpeed = 3f;
    [Tooltip("Position dip per m/s of fall speed.")]
    [SerializeField] private float landingDip = 0.015f;
    [Tooltip("Pitch kick (deg) per m/s of fall speed.")]
    [SerializeField] private float landingKick = 0.6f;
    [Tooltip("Shake trauma added per m/s of fall speed.")]
    [SerializeField] private float landingShake = 0.04f;
    [SerializeField] private float impactStiffness = 120f;
    [SerializeField] private float impactDamping = 12f;

    [Header("Handheld Noise")]
    [SerializeField] private float idleSway = 0.5f;
    [SerializeField] private float idleSwaySpeed = 0.3f;
    [SerializeField] private float jitter = 0.12f;
    [SerializeField] private float jitterSpeed = 8f;

    [Header("Shake (trauma)")]
    [SerializeField] private float maxShakeAngle = 3.5f;
    [SerializeField] private float maxShakeOffset = 0.04f;
    [SerializeField] private float shakeFrequency = 20f;
    [Tooltip("Trauma lost per second.")]
    [SerializeField] private float traumaDecay = 1.4f;

    /// <summary>Fires on every footstep while grounded and moving. Hook footstep audio here.</summary>
    public event Action Footstep;

    public float Trauma => trauma;
    public float HorizontalSpeed => horizontalSpeed;
    public bool Grounded => grounded;

    /// <summary>Add camera shake. 0.1 = light bump, 0.4 = nearby explosion, 1 = max.</summary>
    public void AddShake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    // ---------------------------------------------------------------------

    private struct Spring
    {
        public float value;
        public float velocity;

        public void Step(float target, float stiffness, float damping, float dt)
        {
            float force = (target - value) * stiffness - velocity * damping;
            velocity += force * dt;
            value += velocity * dt;
        }
    }

    private Camera cam;
    private float originalFOV;

    private Vector3 cleanLocalPos, appliedLocalPos;
    private Quaternion cleanLocalRot, appliedLocalRot;
    private bool applied;

    private Vector3 lastPos;
    private Quaternion lastRot;
    private bool hasHistory;

    private Vector3 smoothedVel;
    private float horizontalSpeed;
    private float strafeSpeed;
    private bool grounded;
    private bool wasAirborne;
    private float fallSpeed;

    private Spring lagYaw, lagPitch, impactY, impactPitch;
    private float roll;
    private float bobPhase;
    private float bobWeight;
    private float trauma;
    private float currentFOV;
    private float seed;

    private float lookPitch, lookYaw;

    private Coroutine restoreRoutine;
    private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

    // ---------------------------------------------------------------------

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        originalFOV = cam.fieldOfView;
        currentFOV = controlFOV ? baseFOV : cam.fieldOfView;

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        seed = UnityEngine.Random.value * 100f;
        hasHistory = false;
        applied = false;

        Vector3 e = transform.localEulerAngles;
        lookPitch = NormalizeAngle(e.x);
        lookYaw = NormalizeAngle(e.y);

        if (useBuiltInMouseLook && lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        restoreRoutine = StartCoroutine(RestoreAtEndOfFrame());
    }

    private void OnDisable()
    {
        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }

        RestoreCleanPose();

        if (controlFOV && cam != null)
            cam.fieldOfView = originalFOV;
    }

    private void Update()
    {
        if (useBuiltInMouseLook)
            HandleMouseLook();
    }

    private void LateUpdate()
    {
        CaptureCleanPose();

        float dt = Time.deltaTime;
        if (dt > 0f)
            Simulate(Mathf.Min(dt, 0.05f));

        ApplyEffects();
    }

    // ---------------------------------------------------------------------
    // Pose management

    private IEnumerator RestoreAtEndOfFrame()
    {
        while (true)
        {
            yield return EndOfFrame;
            RestoreCleanPose();
        }
    }

    private void RestoreCleanPose()
    {
        if (!applied) return;
        transform.localPosition = cleanLocalPos;
        transform.localRotation = cleanLocalRot;
        applied = false;
    }

    private void CaptureCleanPose()
    {
        // Fallback if the end-of-frame restore didn't run (e.g. Game view hidden in the editor):
        // only undo our offset if nothing else has moved the camera since we applied it.
        if (applied)
        {
            if (transform.localPosition == appliedLocalPos) transform.localPosition = cleanLocalPos;
            if (transform.localRotation == appliedLocalRot) transform.localRotation = cleanLocalRot;
            applied = false;
        }

        cleanLocalPos = transform.localPosition;
        cleanLocalRot = transform.localRotation;
    }

    // ---------------------------------------------------------------------
    // Simulation

    private void Simulate(float dt)
    {
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        if (!hasHistory)
        {
            lastPos = pos;
            lastRot = rot;
            hasHistory = true;
        }

        // --- Velocity (from the clean camera position, so it works with any controller)
        Vector3 rawVel = (pos - lastPos) / dt;
        if (rawVel.sqrMagnitude > 50f * 50f) rawVel = smoothedVel; // teleport guard
        smoothedVel = Vector3.Lerp(smoothedVel, rawVel, 1f - Mathf.Exp(-12f * dt));

        Vector3 flatFwd = FlatForward(rot);
        Vector3 flatRight = Vector3.Cross(Vector3.up, flatFwd);
        Vector3 horizVel = Vector3.ProjectOnPlane(smoothedVel, Vector3.up);
        horizontalSpeed = horizVel.magnitude;
        strafeSpeed = Vector3.Dot(horizVel, flatRight);

        grounded = characterController != null
            ? characterController.isGrounded
            : Mathf.Abs(smoothedVel.y) < 1.5f;

        // --- Landing
        if (!grounded)
        {
            fallSpeed = Mathf.Max(fallSpeed, -rawVel.y);
            wasAirborne = true;
        }
        else if (wasAirborne)
        {
            if (fallSpeed > landingMinSpeed)
            {
                float f = Mathf.Min(fallSpeed, 15f);
                Kick(ref impactY, -landingDip * f, impactStiffness);
                Kick(ref impactPitch, landingKick * f, impactStiffness);
                AddShake(landingShake * f);
            }
            fallSpeed = 0f;
            wasAirborne = false;
        }

        // --- Look delta
        Vector3 f0 = lastRot * Vector3.forward;
        Vector3 f1 = rot * Vector3.forward;
        float yawDelta = Mathf.DeltaAngle(YawOf(f0), YawOf(f1));
        float pitchDelta = Mathf.DeltaAngle(PitchOf(f0), PitchOf(f1));
        if (Mathf.Abs(yawDelta) > 90f || Mathf.Abs(pitchDelta) > 90f)
        {
            // Snap/teleport rotation: don't treat it as a fast turn.
            yawDelta = 0f;
            pitchDelta = 0f;
        }

        // --- Look lag: view trails the input, then springs back (with slight overshoot)
        lagYaw.value = Mathf.Clamp(lagYaw.value - yawDelta * lagAmount, -maxLagAngle, maxLagAngle);
        lagPitch.value = Mathf.Clamp(lagPitch.value - pitchDelta * lagAmount, -maxLagAngle, maxLagAngle);
        lagYaw.Step(0f, lagStiffness, lagDamping, dt);
        lagPitch.Step(0f, lagStiffness, lagDamping, dt);

        // --- Roll from turning + strafing
        float yawRate = yawDelta / dt;
        float targetRoll = Mathf.Clamp(-yawRate * turnRoll, -maxTurnRoll, maxTurnRoll) - strafeSpeed * strafeRoll;
        roll = Mathf.Lerp(roll, targetRoll, 1f - Mathf.Exp(-rollSmoothing * dt));

        // --- Walk bob
        float moveRatio = walkSpeed > 0.01f ? horizontalSpeed / walkSpeed : 0f;
        bool walking = grounded && horizontalSpeed > 0.3f;
        float targetWeight = walking ? Mathf.Clamp(moveRatio, 0f, 1.5f) : 0f;
        bobWeight = Mathf.Lerp(bobWeight, targetWeight, 1f - Mathf.Exp(-8f * dt));

        if (walking)
        {
            float freq = stepsPerSecond * Mathf.Clamp(Mathf.Sqrt(moveRatio), 0.5f, 1.4f);
            float prev = bobPhase;
            bobPhase += freq * Mathf.PI * dt; // one footstep every PI radians

            if (Mathf.Floor(bobPhase / Mathf.PI) != Mathf.Floor(prev / Mathf.PI))
                OnFootstep();

            if (bobPhase >= Mathf.PI * 2f)
                bobPhase -= Mathf.PI * 2f;
        }

        // --- Impact springs
        impactY.Step(0f, impactStiffness, impactDamping, dt);
        impactPitch.Step(0f, impactStiffness, impactDamping, dt);

        // --- Shake decay
        trauma = Mathf.Max(0f, trauma - traumaDecay * dt);

        // --- FOV
        if (controlFOV)
        {
            float sprint01 = sprintSpeed > walkSpeed
                ? Mathf.Clamp01((horizontalSpeed - walkSpeed) / (sprintSpeed - walkSpeed))
                : 0f;
            float targetFOV = baseFOV + sprintFOVBoost * sprint01 * intensity;
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, 1f - Mathf.Exp(-fovSmoothing * dt));
        }

        lastPos = pos;
        lastRot = rot;
    }

    private void OnFootstep()
    {
        Kick(ref impactPitch, stepKick * bobWeight, impactStiffness);
        Kick(ref impactY, -stepDip * bobWeight, impactStiffness);
        Footstep?.Invoke();
    }

    // ---------------------------------------------------------------------
    // Apply

    private void ApplyEffects()
    {
        float k = intensity;
        float t = Time.time;
        float w = bobWeight;
        float s = Mathf.Sin(bobPhase);
        float absS = Mathf.Abs(s);
        float shake = trauma * trauma;
        float idleScale = 1f - 0.6f * Mathf.Clamp01(w); // less idle sway while walking

        // Position (facing space)
        Vector3 offset = mountOffset;
        offset.x += s * bobSideways * w * k;
        offset.y += (absS - 0.5f) * 2f * bobVertical * w * k;
        offset.y += impactY.value * k;
        offset += new Vector3(
            Noise(1, t * shakeFrequency),
            Noise(2, t * shakeFrequency),
            Noise(3, t * shakeFrequency)) * (maxShakeOffset * shake * k);

        // Rotation (degrees)
        float pitch =
            lagPitch.value +
            impactPitch.value +
            Mathf.Cos(bobPhase * 2f) * bobPitch * w +
            Noise(4, t * idleSwaySpeed) * idleSway * idleScale +
            Noise(5, t * jitterSpeed) * jitter +
            Noise(6, t * shakeFrequency) * maxShakeAngle * shake;

        float yaw =
            lagYaw.value +
            Noise(7, t * idleSwaySpeed) * idleSway * idleScale +
            Noise(8, t * jitterSpeed) * jitter +
            Noise(9, t * shakeFrequency) * maxShakeAngle * shake;

        float rollTotal =
            roll +
            s * bobRoll * w +
            Noise(10, t * idleSwaySpeed) * idleSway * 0.5f * idleScale +
            Noise(11, t * shakeFrequency) * maxShakeAngle * shake;

        pitch *= k;
        yaw *= k;
        rollTotal *= k;

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        Quaternion facing = Quaternion.LookRotation(FlatForward(rot), Vector3.up);

        transform.position = pos + facing * offset;
        transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * rot * Quaternion.Euler(pitch, 0f, rollTotal);

        if (controlFOV)
            cam.fieldOfView = currentFOV;

        appliedLocalPos = transform.localPosition;
        appliedLocalRot = transform.localRotation;
        applied = true;
    }

    // ---------------------------------------------------------------------
    // Built-in mouse look

    private void HandleMouseLook()
    {
        Vector2 d = ReadLookInput() * mouseSensitivity;
        lookPitch = Mathf.Clamp(lookPitch - d.y, -pitchLimit, pitchLimit);

        if (playerBody != null && playerBody != transform)
        {
            playerBody.Rotate(Vector3.up, d.x, Space.Self);
            transform.localRotation = Quaternion.Euler(lookPitch, 0f, 0f);
        }
        else
        {
            lookYaw += d.x;
            transform.localRotation = Quaternion.Euler(lookPitch, lookYaw, 0f);
        }
    }

    private static Vector2 ReadLookInput()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current == null) return Vector2.zero;
        return Mouse.current.delta.ReadValue() * 0.1f; // roughly matches legacy "Mouse X/Y" scale
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    // ---------------------------------------------------------------------
    // Helpers

    private static void Kick(ref Spring spring, float peak, float stiffness)
    {
        // Impulse sized so the spring's first swing peaks at roughly 'peak'.
        spring.velocity += peak * Mathf.Sqrt(stiffness);
    }

    private float Noise(int channel, float x)
    {
        return (Mathf.PerlinNoise(seed + channel * 13.37f, x) - 0.5f) * 2f;
    }

    private static Vector3 FlatForward(Quaternion rot)
    {
        Vector3 f = Vector3.ProjectOnPlane(rot * Vector3.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-4f)
            f = Vector3.ProjectOnPlane(rot * Vector3.up, Vector3.up); // looking straight up/down
        return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized;
    }

    private static float YawOf(Vector3 f) => Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
    private static float PitchOf(Vector3 f) => -Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
    private static float NormalizeAngle(float a) => a > 180f ? a - 360f : a;
}