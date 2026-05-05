using UnityEngine;

// Third-person chase: camera offset is entirely in the player’s local space (it rides pitch/yaw/roll with them).
// Always looks at a focus point on the glider so the player stays centered — no separate mouse orbit.
public class CameraTracking : MonoBehaviour
{
    [Tooltip("Glider / Rigidbody transform. Auto-filled from GlidingSystem if empty.")]
    [SerializeField] private Transform player;

    [Tooltip("Where the camera aims (player-local). Typically chest / nose height + slightly forward.")]
    [SerializeField] private Vector3 lookFocusLocal = new Vector3(0f, 0.7f, 30f);

    [Tooltip("Camera position in player-local space: Y up, Z negative = behind the glider.")]
    [SerializeField] private Vector3 cameraOffsetLocal = new Vector3(0f, 1.3f, -5f);

    [SerializeField] private bool autoFindPlayer = true;

    private bool warnedMissing;

    private void Awake()
    {
        if (player == null && autoFindPlayer)
        {
            GlidingSystem glider = Object.FindAnyObjectByType<GlidingSystem>();
            if (glider != null)
                player = GlidingSystem.GetPhysicsFollowTransform(glider);
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            if (!warnedMissing)
            {
                warnedMissing = true;
                Debug.LogWarning($"{nameof(CameraTracking)} on '{name}': assign Player or add {nameof(GlidingSystem)}.", this);
            }

            return;
        }

        Vector3 focus = player.position + player.TransformDirection(lookFocusLocal);
        Vector3 camPos = player.position + player.TransformDirection(cameraOffsetLocal);

        transform.position = camPos;

        Vector3 toFocus = focus - transform.position;
        if (toFocus.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.LookRotation(toFocus.normalized, player.up);
    }
}
