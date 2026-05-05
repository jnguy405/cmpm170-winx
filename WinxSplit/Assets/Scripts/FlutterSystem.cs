using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Third-person paper-glider: mouse steers yaw / pitch / light roll on the Rigidbody.
// Aerodynamics: drag along velocity, lift with speed, extra push when diving.
public class GlidingSystem : MonoBehaviour
{
    [Header("Mouse steering")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxPitch = 55f;
    [SerializeField] private float maxRoll = 45f;
    [Tooltip("How strongly horizontal mouse banks the wings toward that side.")]
    [SerializeField] private float rollBankStrength = 42f;
    [SerializeField] private float rollEaseSpeed = 10f;

    [SerializeField] private float horizontalInputSmoothTime = 0.075f;

    [SerializeField] private float verticalInputSmoothTime = 0.5f;

    [SerializeField] private float steerMaxDegreesPerSecond = 30f;

    [Header("Paper glide — forces (Acceleration)")]
    [SerializeField] private float quadraticDrag = 0.7f;
    [SerializeField] private float liftPerSpeed = 3.5f;
    [SerializeField] private float stallSpeed = 3.5f;
    [SerializeField] private float diveBoost = 12f;

    private Rigidbody rb;
    private float yaw;
    private float pitch;
    private float roll;

    private float horizontalInputSmoothed;
    private float horizontalInputVel;
    private float verticalInputSmoothed;
    private float verticalInputVel;

    public static Transform GetPhysicsFollowTransform(GlidingSystem glider)
    {
        if (glider == null)
            return null;

        Rigidbody found = glider.GetComponent<Rigidbody>()
            ?? glider.GetComponentInChildren<Rigidbody>()
            ?? glider.GetComponentInParent<Rigidbody>();

        return found != null ? found.transform : glider.transform;
    }

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

        float hSmooth = horizontalInputSmoothTime;
        horizontalInputSmoothed = SmoothInputAxis(
            horizontalInputSmoothed,
            rawHorizontal,
            ref horizontalInputVel,
            hSmooth,
            Time.deltaTime);

        float vSmooth = verticalInputSmoothTime;
        verticalInputSmoothed = SmoothInputAxis(
            verticalInputSmoothed,
            rawVertical,
            ref verticalInputVel,
            vSmooth,
            Time.deltaTime);

        yaw += horizontalInputSmoothed;
        pitch = Mathf.Clamp(pitch - verticalInputSmoothed, -maxPitch, maxPitch);

        float desiredRoll = Mathf.Clamp(-horizontalInputSmoothed * rollBankStrength, -maxRoll, maxRoll);
        float rollAlpha = 1f - Mathf.Exp(-rollEaseSpeed * Time.deltaTime);
        roll = Mathf.Lerp(roll, desiredRoll, rollAlpha);
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        Quaternion targetOrient = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, roll);
        float maxStep = steerMaxDegreesPerSecond <= 1e-5f
            ? Mathf.Infinity
            : steerMaxDegreesPerSecond * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetOrient, maxStep));

        ApplyPaperGlideForces();
    }

    private void ApplyPaperGlideForces()
    {
        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        Vector3 forward = transform.forward;

        if (speed > 0.01f)
            rb.AddForce(-quadraticDrag * speed * v, ForceMode.Acceleration);

        float air = Mathf.Clamp01(speed / Mathf.Max(0.01f, stallSpeed));
        float lift = liftPerSpeed * speed * air * Mathf.Clamp01(Vector3.Dot(transform.up, Vector3.up));
        rb.AddForce(Vector3.up * lift, ForceMode.Acceleration);

        float dive = Mathf.Max(0f, Vector3.Dot(forward, Vector3.down));
        rb.AddForce(forward * (diveBoost * dive * air), ForceMode.Acceleration);
    }

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
}
