using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GlidingSystem : MonoBehaviour
{
    [SerializeField] private float BaseSpeed = 30f;
    [SerializeField] private float MaxThrustSpeed = 120f;
    [Tooltip("Minimum speed (m/s) before full glide drag is applied; glide force still applies below this.")]
    [SerializeField] private float MinThrustSpeed = 2f;
    [SerializeField] private float ThrustFactor = 40f;
    [SerializeField] private float DragFactor = 2f;
    [SerializeField] private float MinDrag = 0.5f;
    [SerializeField] private float RotationSpeed;
    [SerializeField] private float TiltStrength = 90;
    [SerializeField] private float LowPercent = 0.1f, HighPercent = 1;

    [Header("Camera")]
    [Tooltip("The transform whose yaw/pitch the player should match — usually the same GameObject that has CameraTracking (your camera rig), not the mesh.")]
    [SerializeField] private Transform cameraOrientationSource;
    
    private float CurrentThrustSpeed;
    private float TiltValue, LerpValue;

    private Transform CameraTransform;
    private Rigidbody Rb;

    // Transform that actually moves with physics — use for camera follow when Rigidbody is not on the same object as GlidingSystem.
    public static Transform GetPhysicsFollowTransform(GlidingSystem glider)
    {
        if (glider == null)
            return null;

        Rigidbody rb = glider.GetComponent<Rigidbody>()
            ?? glider.GetComponentInChildren<Rigidbody>()
            ?? glider.GetComponentInParent<Rigidbody>();

        return rb != null ? rb.transform : glider.transform;
    }

    private Transform BodyTransform => Rb != null ? Rb.transform : transform;

    private void Awake()
    {
        ResolveRigidbody();
    }

    private void Start()
    {
        if (cameraOrientationSource != null)
            CameraTransform = cameraOrientationSource;
        else if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            CameraTransform = cam.parent != null ? cam.parent : cam;
        }

        ResolveRigidbody();

        if (MaxThrustSpeed <= 0f)
            MaxThrustSpeed = 120f;
        if (ThrustFactor <= 0f)
            ThrustFactor = 40f;
        if (DragFactor <= 0f)
            DragFactor = 2f;

        CurrentThrustSpeed = Mathf.Clamp(BaseSpeed, 0f, MaxThrustSpeed);

        if (CameraTransform == null)
            Debug.LogWarning(
                $"{nameof(GlidingSystem)} on '{name}': drag the camera rig into Camera Orientation Source (same object as CameraTracking), or ensure a camera is tagged MainCamera.",
                this);

        if (Rb == null)
            Debug.LogWarning(
                $"{nameof(GlidingSystem)} on '{name}': add a Rigidbody on this object or a parent/child so the prefab moves with physics.",
                this);
    }

    private void ResolveRigidbody()
    {
        Rb = GetComponent<Rigidbody>()
            ?? GetComponentInChildren<Rigidbody>()
            ?? GetComponentInParent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        GlidingMovement();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        ManageRotation();
    }

    private void GlidingMovement() 
    {
        if (Rb == null)
            return;

        Transform body = BodyTransform;
        float pitchInDeg = body.eulerAngles.x % 360;
        float pitchInRads = body.eulerAngles.x * Mathf.Deg2Rad;
        float mappedPitch = -Mathf.Sin(pitchInRads);
        float offsetMappedPitch = Mathf.Cos(pitchInRads) * DragFactor;
        float accelerationPercent = pitchInDeg >= 300f ? LowPercent : HighPercent; 
        Vector3 glidingForce = -Vector3.forward * CurrentThrustSpeed;

        CurrentThrustSpeed += mappedPitch * accelerationPercent * ThrustFactor * Time.fixedDeltaTime;
        CurrentThrustSpeed = Mathf.Clamp(CurrentThrustSpeed, 0f, MaxThrustSpeed);

        if (CurrentThrustSpeed > 0.001f)
            Rb.AddRelativeForce(glidingForce);

        if (Rb.linearVelocity.magnitude >= MinThrustSpeed)
            Rb.linearDamping = Mathf.Clamp(offsetMappedPitch, MinDrag, DragFactor);
        else
            Rb.linearDamping = MinDrag;
    }
    private void ManageRotation() 
    {
        if (CameraTransform == null)
            return;

        float mouseX = 0f;
        if (Mouse.current != null)
        {
            const float pixelScale = 0.02f;
            mouseX = Mouse.current.delta.ReadValue().x * pixelScale;
        }

        TiltValue += mouseX * TiltStrength;

        if (Mathf.Abs(mouseX) < 1e-4f)
        {
            TiltValue = Mathf.Lerp(TiltValue, 0, LerpValue);
            LerpValue += Time.deltaTime;
        }
        else 
        {
            LerpValue = 0;
        }

        Quaternion targetRotation = Quaternion.Euler(CameraTransform.eulerAngles.x, CameraTransform.eulerAngles.y, TiltValue);
        Transform body = BodyTransform;
        body.rotation = Quaternion.Lerp(body.rotation, targetRotation, RotationSpeed * Time.deltaTime);
    }
}