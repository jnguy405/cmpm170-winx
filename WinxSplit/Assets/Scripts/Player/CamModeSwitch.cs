using UnityEngine;

public class CamModeSwitch : MonoBehaviour
{
    [Header("Camera Controllers")]
    [SerializeField] private GameObject thirdPersonCameraRoot;
    [SerializeField] private GameObject glidingCameraRoot;
    [SerializeField] private bool switchCameraGameObjects = true;

    [Header("Glide source")]
    [SerializeField] private GlidingSystem glidingSystem;
    [SerializeField] private bool isGliding;

    private ThirdPersonCam thirdPersonCamera;
    private CameraTracking glidingCamera;

    private bool initialized;

    private void Awake()
    {
        CacheCameraComponents();
    }

    private void OnEnable()
    {
        ApplyCameraState(GetDesiredGlideState(), true);
    }

    private void Update()
    {
        ApplyCameraState(GetDesiredGlideState(), false);
    }

    public void SetGliding(bool value)
    {
        isGliding = value;
        if (glidingSystem != null)
        {
            glidingSystem.SetGliding(value);
        }

        ApplyCameraState(value, true);
    }

    private bool GetDesiredGlideState()
    {
        if (glidingSystem != null)
        {
            return glidingSystem.IsGliding;
        }

        return isGliding;
    }

    private void CacheCameraComponents()
    {
        if (thirdPersonCamera == null && thirdPersonCameraRoot != null)
        {
            thirdPersonCamera = thirdPersonCameraRoot.GetComponent<ThirdPersonCam>();
        }

        if (glidingCamera == null && glidingCameraRoot != null)
        {
            glidingCamera = glidingCameraRoot.GetComponent<CameraTracking>();
        }
    }

    private void ApplyCameraState(bool gliding, bool force)
    {
        if (!force && initialized && gliding == isGliding)
        {
            return;
        }

        bool previousGliding = isGliding;
        isGliding = gliding;
        initialized = true;

        if (previousGliding != gliding)
        {
            AlignIncomingCameraPose(gliding);
        }

        if (switchCameraGameObjects)
        {
            if (thirdPersonCameraRoot != null)
            {
                thirdPersonCameraRoot.SetActive(!gliding);
            }

            if (glidingCameraRoot != null)
            {
                glidingCameraRoot.SetActive(gliding);
            }
        }

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = !gliding;
            thirdPersonCamera.SetMouseLookEnabled(!gliding);
        }

        if (glidingCamera != null)
        {
            glidingCamera.enabled = gliding;
        }
    }

    private void AlignIncomingCameraPose(bool gliding)
    {
        Transform outgoing = gliding ? GetThirdPersonTransform() : GetGlidingTransform();
        Transform incoming = gliding ? GetGlidingTransform() : GetThirdPersonTransform();
        if (incoming == null || outgoing == null)
        {
            return;
        }

        incoming.SetPositionAndRotation(outgoing.position, outgoing.rotation);
    }

    private Transform GetThirdPersonTransform()
    {
        if (thirdPersonCameraRoot != null)
        {
            return thirdPersonCameraRoot.transform;
        }

        return thirdPersonCamera != null ? thirdPersonCamera.transform : null;
    }

    private Transform GetGlidingTransform()
    {
        if (glidingCameraRoot != null)
        {
            return glidingCameraRoot.transform;
        }

        return glidingCamera != null ? glidingCamera.transform : null;
    }
}
