using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Third-person paper-glider: mouse steers yaw / pitch / light roll on the Rigidbody.
// Aerodynamics: drag along velocity, lift with speed, extra push when diving.
public class GlidingSystem : MonoBehaviour
{
    [Header("Glide State")]
    [SerializeField] private bool isGliding;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float minGroundNormalY = 0.2f;

    [Header("Mouse steering")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxPitch = 55f;                      // Maximum pitch of the glider (angle of the glider in the y-axis)
    [SerializeField] private float maxRoll = 45f;                       // Maximum roll of the glider (angle of the glider in the z-axis)
    [SerializeField] private float rollBankStrength = 30f;              // Strength of the roll bank (how much the roll bank is applied)
    [SerializeField] private float rollEaseSpeed = 10f;                 // Speed of the roll ease (how fast the roll ease is applied)

    [SerializeField] private float horizontalInputSmoothTime = 0.075f;  // Smooth time of the horizontal input (how smooth the horizontal input is)

    [SerializeField] private float verticalInputSmoothTime = 0.5f;      // Smooth time of the vertical input (how smooth the vertical input is)

    [SerializeField] private float steerMaxDegreesPerSecond = 30f;      // Maximum degrees per second of the steering (how fast the steering is applied)

    [Header("Vertical pitch feel (mouse up/down)")]
    [SerializeField] private float pullUpPitchInputScale = 1f;          // Scale of the pull up pitch input (how much the pull up pitch input is applied)

    [SerializeField] private float divePitchInputScale = 1.2f;          // Scale of the dive pitch input (how much the dive pitch input is applied)

    [Header("Paper glide — forces (Acceleration)")]
    [SerializeField] private float forwardAcceleration = 500f;          // Acceleration of the forward force (how fast the forward force is applied)

    [SerializeField] private float quadraticDrag = 0.2f;                // Drag of the quadratic drag (how much the quadratic drag is applied)
    [SerializeField] private float liftPerSpeed = 3.5f;                 // Lift per speed (how much the lift is applied per speed)
    [SerializeField] private float stallSpeed = 3.5f;                   // Stall speed (the speed at which the stall is applied)
    [SerializeField] private float diveBoost = 12f;                     // Boost of the dive (how much the dive is applied)
    [SerializeField] private float diveDropAcceleration = 100f;         // Acceleration of the dive drop (how fast the dive drop is applied)
    [SerializeField] private float loftLiftWhenNoseUp = 90f;            // Lift when the nose is up (how much the lift is applied when the nose is up)

    private Rigidbody rb;                                      // Rigidbody of the glider
    private float yaw;                                         // Yaw of the glider (rotation around the y-axis)
    private float pitch;                                       // Pitch of the glider (rotation around the x-axis)
    private float roll;                                        // Roll of the glider (rotation around the z-axis)

    private float horizontalInputSmoothed;                     // Smoothed horizontal input (how smooth the horizontal input is)
    private float horizontalInputVel;                          // Velocity of the horizontal input (how fast the horizontal input is applied)
    private float verticalInputSmoothed;                       // Smoothed vertical input (how smooth the vertical input is)
    private float verticalInputVel;                            // Velocity of the vertical input (how fast the vertical input is applied)

    public bool IsGliding => isGliding;

    // Get the transform of the physics follow object to follow the glider
    public static Transform GetPhysicsFollowTransform(GlidingSystem glider)
    {
        if (glider == null)
            return null;

        Rigidbody found = glider.GetComponent<Rigidbody>()
            ?? glider.GetComponentInChildren<Rigidbody>()
            ?? glider.GetComponentInParent<Rigidbody>();

        return found != null ? found.transform : glider.transform;
    }

    // Check if the glider has a rigidbody and set the interpolation and gravity
    private void Awake()
    {
        rb = GetComponent<Rigidbody>()
            ?? GetComponentInChildren<Rigidbody>()
            ?? GetComponentInParent<Rigidbody>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = true;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 3f;
        }
    }
    
    // Start the glider and set the yaw and pitch (initial orientation)
    private void Start()
    {
        if (rb == null)
        {
            Debug.LogWarning($"{nameof(GlidingSystem)} on '{name}': add a Rigidbody.", this);
            return;
        }

        if (rb.isKinematic)
            Debug.LogWarning($"{nameof(GlidingSystem)} on '{name}': Rigidbody is kinematic.", this);

        Vector3 flat = transform.forward;
        flat.y = 0f;
        if (flat.sqrMagnitude > 1e-4f)
            yaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;

        float x = transform.eulerAngles.x;
        if (x > 180f)
            x -= 360f;
        pitch = Mathf.Clamp(x, -maxPitch, maxPitch);
    }

    // Update the glider and set the yaw and pitch (orientation) corresponding to the mouse input
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (Mouse.current == null || rb == null)
            return;

        Vector2 d = Mouse.current.delta.ReadValue();
        const float pixelScale = 0.02f;
        float s = mouseSensitivity * pixelScale;

        float rawHorizontal = d.x * s;
        float rawVertical = d.y * s;

        // Smooth the horizontal input
        float hSmooth = horizontalInputSmoothTime;
        horizontalInputSmoothed = SmoothInputAxis(
            horizontalInputSmoothed,
            rawHorizontal,
            ref horizontalInputVel,
            hSmooth,
            Time.deltaTime);

        // Smooth the vertical input
        float vSmooth = verticalInputSmoothTime;
        verticalInputSmoothed = SmoothInputAxis(
            verticalInputSmoothed,
            rawVertical,
            ref verticalInputVel,
            vSmooth,
            Time.deltaTime);

        // Update the yaw
        yaw += horizontalInputSmoothed;

        // Update the pitch
        float v = verticalInputSmoothed;
        float pitchInput = v;
        if (Mathf.Abs(v) > 1e-6f)
            pitchInput = v > 0f ? v * pullUpPitchInputScale : v * divePitchInputScale;

        pitch = Mathf.Clamp(pitch - pitchInput, -maxPitch, maxPitch);

        // Update the roll based on the horizontal input
        float desiredRoll = Mathf.Clamp(-horizontalInputSmoothed * rollBankStrength, -maxRoll, maxRoll);
        float rollAlpha = 1f - Mathf.Exp(-rollEaseSpeed * Time.deltaTime);
        roll = Mathf.Lerp(roll, desiredRoll, rollAlpha);
    }

    // Fixed update the glider and apply the paper glide forces
    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (!isGliding)
            return;

        // Uses Euler angles to rotate the glider to the target orientation
        Quaternion targetOrient = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, roll);
        float maxStep = steerMaxDegreesPerSecond <= 1e-5f
            ? Mathf.Infinity
            : steerMaxDegreesPerSecond * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetOrient, maxStep));

        ApplyPaperGlideForces();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryExitGlideOnGroundCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryExitGlideOnGroundCollision(collision);
    }

    private void TryExitGlideOnGroundCollision(Collision collision)
    {
        if (!isGliding || collision == null)
            return;

        if ((groundMask.value & (1 << collision.gameObject.layer)) == 0)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (contact.normal.y >= minGroundNormalY)
            {
                isGliding = false;
                return;
            }
        }
    }

    // Apply the paper glide forces to the glider
    private void ApplyPaperGlideForces()
    {
        Vector3 v = rb.linearVelocity;
        // Speed of the glider
        float speed = v.magnitude;
        Vector3 forward = transform.forward;

        if (forwardAcceleration > 1e-5f)
            rb.AddForce(forward * forwardAcceleration, ForceMode.Acceleration);

        if (speed > 0.01f)
            rb.AddForce(-quadraticDrag * speed * v, ForceMode.Acceleration);

        // Calculate the air density
        float air = Mathf.Clamp01(speed / Mathf.Max(0.01f, stallSpeed));
        
        // Calculate the lift force based on the speed and air density
        float lift = liftPerSpeed * speed * air * Mathf.Clamp01(Vector3.Dot(transform.up, Vector3.up));
        rb.AddForce(Vector3.up * lift, ForceMode.Acceleration);

        // Calculate the dive force based on the forward direction and the speed
        float dive = Mathf.Max(0f, Vector3.Dot(forward, Vector3.down));
        rb.AddForce(forward * (diveBoost * dive * air), ForceMode.Acceleration);

        if (diveDropAcceleration > 1e-5f)
            rb.AddForce(Vector3.down * (diveDropAcceleration * dive * air), ForceMode.Acceleration);

        float noseUp = Mathf.Max(0f, Vector3.Dot(forward, Vector3.up));
        if (loftLiftWhenNoseUp > 1e-5f)
            rb.AddForce(Vector3.up * (loftLiftWhenNoseUp * noseUp * air), ForceMode.Acceleration);
    }

    // Smooth the input axis by "damping" the input towards the target value
    private static float SmoothInputAxis(float current, float target, ref float velocity, float smoothTime, float deltaTime)
    {
        if (smoothTime <= 1e-5f)
        {
            velocity = 0f;
            return target;
        }

        return Mathf.SmoothDamp(
            current,
            target,
            ref velocity,
            Mathf.Max(1e-5f, smoothTime),
            Mathf.Infinity,
            deltaTime);
    }

    public void SetGliding(bool value)
    {
        isGliding = value;
    }
}
