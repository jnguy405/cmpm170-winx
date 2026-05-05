using UnityEngine;

public class CamModeSwitch : MonoBehaviour
{
    [Header("Camera Controllers")]
    [SerializeField] private GameObject thirdPersonCameraRoot;
    [SerializeField] private GameObject glidingCameraRoot;
    [SerializeField] private bool switchCameraGameObjects = true;

    [Header("Glide Source")]
    [SerializeField] private GlidingSystem glidingSystem;
    [SerializeField] private bool isGliding;
    [SerializeField] private bool autoFindReferences = true;

    private ThirdPersonCam thirdPersonCamera;
    private CameraTracking glidingCamera;
    private bool initialized;

    private void Awake()
    {
        CacheCameraComponents();

        if (autoFindReferences)
        {
            if (thirdPersonCameraRoot == null && thirdPersonCamera != null)
            {
                thirdPersonCameraRoot = thirdPersonCamera.gameObject;
            }

            if (glidingCameraRoot == null && glidingCamera != null)
            {
                glidingCameraRoot = glidingCamera.gameObject;
            }

            if (glidingSystem == null)
            {
                glidingSystem = FindAnyObjectByType<GlidingSystem>();
            }
        }
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

        if (!autoFindReferences)
        {
            return;
        }

        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = GetComponent<ThirdPersonCam>() ?? FindAnyObjectByType<ThirdPersonCam>();
        }

        if (glidingCamera == null)
        {
            glidingCamera = GetComponent<CameraTracking>() ?? FindAnyObjectByType<CameraTracking>();
        }
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

    private void ApplyCameraState(bool gliding, bool force)
    {
        if (!force && initialized && gliding == isGliding)
        {
            return;
        }

        isGliding = gliding;
        initialized = true;

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
        }

        if (glidingCamera != null)
        {
            glidingCamera.enabled = gliding;
        }
    }
}
