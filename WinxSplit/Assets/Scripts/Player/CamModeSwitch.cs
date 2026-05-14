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
    private Camera thirdPersonUnityCamera;
    private Camera glidingUnityCamera;

    private bool initialized;

    private void Awake()
    {
        CacheCameraComponents();
    }

    private void OnEnable()
    {
        ApplyCameraState(GetDesiredGlideState(), true);
    }

    public bool IsGlideCameraActive => isGliding;

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

        if (thirdPersonUnityCamera == null && thirdPersonCameraRoot != null)
        {
            thirdPersonUnityCamera = thirdPersonCameraRoot.GetComponent<Camera>();
        }

        if (glidingUnityCamera == null && glidingCameraRoot != null)
        {
            glidingUnityCamera = glidingCameraRoot.GetComponent<Camera>();
        }
    }

    private void ApplyUnityCameraModes(bool gliding)
    {
        if (thirdPersonUnityCamera != null)
        {
            bool active = !gliding;
            thirdPersonUnityCamera.enabled = active;
            thirdPersonUnityCamera.depth = active ? 0f : -10f;
            if (active)
            {
                thirdPersonUnityCamera.clearFlags = CameraClearFlags.Skybox;
            }
        }

        if (glidingUnityCamera != null)
        {
            bool active = gliding;
            glidingUnityCamera.enabled = active;
            glidingUnityCamera.depth = active ? 0f : -10f;
            if (active)
            {
                glidingUnityCamera.clearFlags = CameraClearFlags.Skybox;
            }
        }
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
            thirdPersonCamera.SetMouseLookEnabled(!gliding);
        }

        if (glidingCamera != null)
        {
            glidingCamera.enabled = gliding;
        }

        ApplyUnityCameraModes(gliding);
    }

}
