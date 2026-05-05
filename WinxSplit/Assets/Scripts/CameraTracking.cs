using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTracking : MonoBehaviour
{
    [Tooltip("Player root (Rigidbody / GlidingSystem). Camera rig follows this plus Offset.")]
    [SerializeField] private Transform PlayerTransform;

    [SerializeField] private float SmoothTime = 0.15f;
    [SerializeField] private float Sensitivity = 2f;
    [SerializeField] private float MaxViewRange = 60f;
    private float mouseX, mouseY;

    [Tooltip("Typical third-person: slightly above and behind the player (e.g. 0, 1.5, -4).")]
    [SerializeField] private Vector3 Offset;

    [Tooltip("If empty, finds the first GlidingSystem and follows its Rigidbody transform (so it matches the mesh that actually moves).")]
    [SerializeField] private bool autoFindPlayerIfMissing = true;

    private Vector3 CurrentVelocity;
    private bool _warnedMissingPlayer;

    private void Awake()
    {
        if (PlayerTransform == null && autoFindPlayerIfMissing)
        {
            GlidingSystem glider = Object.FindAnyObjectByType<GlidingSystem>();
            if (glider != null)
                PlayerTransform = GlidingSystem.GetPhysicsFollowTransform(glider);
        }
    }

    private void LateUpdate()
    {
        FollowTargetTransform();
    }

    private void Update()
    {
        CameraRotation();
    }

    private void FollowTargetTransform()
    {
        if (PlayerTransform == null)
        {
            if (!_warnedMissingPlayer)
            {
                _warnedMissingPlayer = true;
                Debug.LogWarning($"{nameof(CameraTracking)} on '{name}': assign Player Transform to the player root.", this);
            }

            return;
        }

        Vector3 desiredPosition = PlayerTransform.position + Offset;
        float smooth = Mathf.Max(SmoothTime, 0.01f);
        Vector3 positionInterpolation = Vector3.SmoothDamp(transform.position, desiredPosition, ref CurrentVelocity, smooth);

        transform.position = positionInterpolation;
    }
    private void CameraRotation()
    {
        if (Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        // Mouse delta is in pixels; scale so existing Sensitivity values stay in a usable range vs. legacy GetAxisRaw.
        const float pixelToAxisScale = 0.02f;
        float scale = Sensitivity * pixelToAxisScale;
        mouseX += delta.y * scale;
        mouseY += delta.x * scale;
        float clampedX = Mathf.Clamp(mouseX, -MaxViewRange, MaxViewRange);

        Quaternion targetRotation = Quaternion.Euler(clampedX, mouseY, transform.eulerAngles.z);
        transform.rotation = targetRotation;
    }
}