using UnityEngine;

// Third-person chase: camera offset is entirely in the player’s local space (it rides pitch/yaw/roll with them).
// Always looks at a focus point on the glider so the player stays centered — no separate mouse orbit.

public class CameraTracking : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private Vector3 lookFocusLocal = new Vector3(0f, 1.55f, 0f);            // Distance from the glider to the focus point

    [SerializeField] private Vector3 cameraOffsetLocal = new Vector3(0f, 1.35f, -2.35f);      // Distance from the glider to the camera

    [SerializeField] private float positionSmooth = 8f;
    [SerializeField] private float rotationSmooth = 10f;

    private void OnEnable()
    {
        UpdateCameraPose(true);
    }

    // Late update the camera and set the position and rotation of the camera
    private void LateUpdate()
    {
        UpdateCameraPose(false);
    }

    private void UpdateCameraPose(bool snap)
    {
        if (player == null)
        {
            return;
        }

        // Calculate the focus point and the camera position
        Vector3 focus = player.position + player.TransformDirection(lookFocusLocal);
        Vector3 desiredPosition = player.position + player.TransformDirection(cameraOffsetLocal);
        if (snap)
        {
            transform.position = desiredPosition;
        }
        else
        {
            float posAlpha = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, posAlpha);
        }

        Vector3 toFocus = focus - transform.position;
        if (toFocus.sqrMagnitude > 1e-6f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(toFocus.normalized, player.up);
            if (snap)
            {
                transform.rotation = desiredRotation;
            }
            else
            {
                float rotAlpha = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotAlpha);
            }
        }
    }
}
