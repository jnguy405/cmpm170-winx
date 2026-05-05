using UnityEngine;
using UnityEngine.InputSystem;

// Cite: Cursor help with rotation and collision input

public class ThirdPersonCam : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Orbit")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float yawSpeed = 180f;
    [SerializeField] private float pitchSpeed = 120f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool invertY;

    [Header("Smoothing")]
    [SerializeField] private float positionSmooth = 12f;
    [SerializeField] private float rotationSmooth = 16f;

    [Header("Collision")]
    [SerializeField] private bool cameraCollision = true;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float collisionBuffer = 0.1f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursor = true;

    private float yaw;
    private float pitch;
    private bool warnedMissingTarget;

    private void Awake()
    {
        if (target == null && autoFindPlayer)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        Vector3 currentAngles = transform.eulerAngles;
        yaw = currentAngles.y;
        pitch = NormalizePitch(currentAngles.x);
    }

    private void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            if (!warnedMissingTarget)
            {
                warnedMissingTarget = true;
                Debug.LogWarning($"{nameof(ThirdPersonCam)} on '{name}': assign a target transform.", this);
            }

            return;
        }

        HandleMouseLook();

        Vector3 focusPoint = target.position + focusOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = focusPoint - orbitRotation * Vector3.forward * distance;

        if (cameraCollision)
        {
            desiredPosition = ResolveCollision(focusPoint, desiredPosition);
        }

        float posAlpha = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, posAlpha);

        Vector3 lookDirection = focusPoint - transform.position;
        if (lookDirection.sqrMagnitude > 1e-6f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            float rotAlpha = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotAlpha);
        }
    }

    private void HandleMouseLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        const float mouseScale = 0.02f;
        float mouseX = delta.x * mouseScale;
        float mouseY = delta.y * mouseScale;

        yaw += mouseX * yawSpeed * Time.deltaTime;

        float pitchInput = invertY ? mouseY : -mouseY;
        pitch = Mathf.Clamp(pitch + pitchInput * pitchSpeed * Time.deltaTime, minPitch, maxPitch);
    }

    private Vector3 ResolveCollision(Vector3 focusPoint, Vector3 desiredPosition)
    {
        Vector3 toCamera = desiredPosition - focusPoint;
        float castDistance = toCamera.magnitude;
        if (castDistance <= 1e-5f)
        {
            return desiredPosition;
        }

        Vector3 castDirection = toCamera / castDistance;
        if (Physics.SphereCast(
            focusPoint,
            collisionRadius,
            castDirection,
            out RaycastHit hit,
            castDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            return focusPoint + castDirection * Mathf.Max(0f, hit.distance - collisionBuffer);
        }

        return desiredPosition;
    }

    private static float NormalizePitch(float rawPitch)
    {
        if (rawPitch > 180f)
        {
            rawPitch -= 360f;
        }

        return rawPitch;
    }
}
