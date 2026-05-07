using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject myCamera;
    [SerializeField] private Transform groundCheck; 
    [SerializeField] private Transform modelRoot;
    [SerializeField] private GlidingSystem glidingSystem;
    [SerializeField] private CamModeSwitch camModeSwitch;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider physicsCollider;
    // [SerializeField] private AudioSource footstepSource;
    // [SerializeField] private AudioClip walkClip;
    // [SerializeField] private AudioClip sprintClip;

    [Header("Movement")]
    [SerializeField] private float groundAcceleration = 35f;
    [SerializeField] private float groundDeceleration = 45f;
    [SerializeField] private float airAcceleration = 12f;
    [SerializeField] private float airDeceleration = 8f;
    [SerializeField] private float jumpHeight = 1.2f;
    private float gravity = -9.81f;

    [Header("Grounding")]
    private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    // private Animator myAnimator;

    // Movement
    private float walkSpeed = 10f;
    private float sprintSpeed = 20f;
    private float crouchSpeed = 5;
    private float turnSpeedDegrees = 720f;
    private float mouseYawSpeed = 180f;
    private float speedMulti = 1f;

    // Movement Input
    private float moveX;
    private float moveZ;
    private bool jumpQueued;

    // Movement State
    private bool isSprinting;
    private bool isWalking;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isCrouching;

    // Game State
    private bool gamePaused;
    private float baseScaleY;
    private const float groundedVerticalClamp = 0f;

    // Camera
    private ThirdPersonCam thirdPersonCam;
    private float pendingYawDelta;


    private void Start()
    {
        // Checks for the player's rigidbody
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>()
                ?? GetComponentInParent<Rigidbody>()
                ?? GetComponentInChildren<Rigidbody>();
        }

        if (rb == null)
        {
            enabled = false;
            return;
        }

        // Checks for the player's camera
        if (myCamera == null && Camera.main != null)
        {
            myCamera = Camera.main.gameObject;
        }

        if (myCamera != null)
        {
            thirdPersonCam = myCamera.GetComponent<ThirdPersonCam>()
                ?? myCamera.GetComponentInParent<ThirdPersonCam>();
            if (thirdPersonCam != null)
            {
                thirdPersonCam.SetMouseLookEnabled(false);
            }
        }

        // Checks for the player's model root
        if (modelRoot == null)
        {
            modelRoot = transform;
        }

        // Checks for the player's physics collider
        if (physicsCollider == null)
        {
            physicsCollider = GetComponent<Collider>()
                ?? rb.GetComponent<Collider>()
                ?? rb.GetComponentInChildren<Collider>();
        }

        if (physicsCollider == null)
        {
            enabled = false;
            return;
        }

        // Sets the player's rigidbody interpolation, collision detection mode, gravity, kinematic, and constraints
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // updates every frame
        rb.useGravity = true;                                                   // the rigidbody is affected by gravity
        rb.isKinematic = false;                                                 // the rigidbody is not kinematic
        rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;                // the rigidbody is not frozen on the y axis

        // myAnimator = GetComponent<Animator>(); // for animations later
        baseScaleY = transform.localScale.y;

        isGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundDistance, groundMask); // checks if the player is grounded
        wasGrounded = isGrounded;                                                                                  // the player was grounded last frame
    }

    private void Update()
    {
        if (gamePaused)
        {
            // StopFootsteps();
            return;
        }

        UpdateGroundedState();
        HandleStateInput();
        HandleMouseLookInput();
        MovementInput();
        // UpdateAnimation();
    }


    // Handles the state input for the player (glide toggle, sprint, crouch)
    private void HandleStateInput()
    {
        Keyboard keyboard = Keyboard.current;
        bool glideTogglePressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;

        if (glideTogglePressed)
        {
            ToggleGlideMode();
        }

        isSprinting = keyboard != null && keyboard.leftCtrlKey.isPressed;

        bool shouldCrouch = keyboard != null && keyboard.leftShiftKey.isPressed;
        if (shouldCrouch != isCrouching)
        {
            Crouch(shouldCrouch);
        }
    }

    // Handles the fixed update for the player (movement, vertical velocity, pending yaw rotation)
    private void FixedUpdate()
    {
        if (gamePaused || rb == null)
        {
            return;
        }

        if (glidingSystem != null && glidingSystem.IsGliding)
        {
            jumpQueued = false;
            return;
        }

        ApplyPendingYawRotation();
        float verticalVelocity = ResolveVerticalVelocity();
        ApplyHorizontalMotion();
        VerticalVelocity(verticalVelocity);
    }

    // Handles the mouse look input for the player (yaw rotation)
    private void HandleMouseLookInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        const float mouseScale = 0.02f;
        float yawDelta = delta.x * mouseScale * mouseYawSpeed * Time.deltaTime;
        pendingYawDelta += yawDelta;
    }

    // Applies the pending yaw rotation to the player
    private void ApplyPendingYawRotation()
    {
        if (Mathf.Abs(pendingYawDelta) < 1e-6f)
        {
            return;
        }

        float maxStep = Mathf.Max(0f, turnSpeedDegrees) * Time.fixedDeltaTime;
        float appliedYaw = Mathf.Clamp(pendingYawDelta, -maxStep, maxStep);
        pendingYawDelta -= appliedYaw;

        Quaternion deltaRotation = Quaternion.Euler(0f, appliedYaw, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    // Updates the grounded state for the player (checks if the player is grounded by sphere cast and collision probe)
    private void UpdateGroundedState()
    {
        bool sphereGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        bool collisionGrounded = IsGroundedByCollisionProbe();
        isGrounded = sphereGrounded || collisionGrounded;

        if (isGrounded && !wasGrounded)
        {
            // Landing should always return from gliding to third-person mode.
            if (glidingSystem != null && glidingSystem.IsGliding)
            {
                glidingSystem.SetGliding(false);
            }

            if (camModeSwitch != null)
            {
                camModeSwitch.SetGliding(false);
            }
        }

        wasGrounded = isGrounded;
    }

    // Resolves the vertical velocity for the player (applies gravity and jump height)
    private float ResolveVerticalVelocity()
    {
        float verticalVelocity = rb.linearVelocity.y;
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedVerticalClamp;

        if (jumpQueued && isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        jumpQueued = false;
        return verticalVelocity;
    }

    // Checks if the player is grounded by a collision probe (used for crouching)
    private bool IsGroundedByCollisionProbe()
    {
        if (physicsCollider == null)
        {
            return false;
        }

        Bounds bounds = physicsCollider.bounds;
        float probeDistance = Mathf.Max(0.02f, groundDistance + 0.05f);
        Vector3 origin = bounds.center;
        float radius = Mathf.Max(0.02f, bounds.extents.x * 0.6f);

        return Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            bounds.extents.y + probeDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }

    // Applies the standard horizontal motion for the player (movement)
    private void ApplyHorizontalMotion()
    {
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move = Vector3.ClampMagnitude(move, 1f);
        isWalking = move.sqrMagnitude > 0.01f;

        float currentSpeed = walkSpeed;
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }

        // Calculates the target planar velocity for the player
        Vector3 targetPlanarVelocity = move * (currentSpeed * speedMulti);
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 currentPlanarVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        bool hasInput = move.sqrMagnitude > 0.0001f;
        float accelRate;
        if (isGrounded)
        {
            accelRate = hasInput ? groundAcceleration : groundDeceleration;
        }
        else
        {
            accelRate = hasInput ? airAcceleration : airDeceleration;
        }

        // Moves the player towards the target planar velocity
        Vector3 nextPlanarVelocity = Vector3.MoveTowards(
            currentPlanarVelocity,
            targetPlanarVelocity,
            Mathf.Max(0f, accelRate) * Time.fixedDeltaTime);

        // Updates the player's linear velocity
        rb.linearVelocity = new Vector3(nextPlanarVelocity.x, currentVelocity.y, nextPlanarVelocity.z);
    }

    // Updates the player based on predefined vertical velocity
    private void VerticalVelocity(float verticalVelocity)
    {
        Vector3 current = rb.linearVelocity;
        rb.linearVelocity = new Vector3(current.x, verticalVelocity, current.z);
    }

    private void MovementInput()
    {
        if (glidingSystem != null && glidingSystem.IsGliding)
        {
            moveX = 0f;
            moveZ = 0f;
            isWalking = false;
            return;
        }

        moveX = AxisFromKeyboard(Key.D, Key.A, Key.RightArrow, Key.LeftArrow);
        moveZ = AxisFromKeyboard(Key.W, Key.S, Key.UpArrow, Key.DownArrow);
        // UpdateFootsteps(move.sqrMagnitude > 0.01f && isGrounded);
    }


    // Gets the axis from the keyboard (positive, negative, positive alt, negative alt which effects the direction of movement)
    private static float AxisFromKeyboard(Key positive, Key negative, Key positiveAlt, Key negativeAlt)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return 0f;
        }

        bool positivePressed = keyboard[positive].isPressed || keyboard[positiveAlt].isPressed;
        bool negativePressed = keyboard[negative].isPressed || keyboard[negativeAlt].isPressed;

        if (positivePressed == negativePressed)
        {
            return 0f;
        }

        return positivePressed ? 1f : -1f;
    }

    // Crouches the player (scales the player's height)
    // probably could be done with a animator later
    private void Crouch(bool crouch)
    {
        isCrouching = crouch;

        Vector3 scale = transform.localScale;
        scale.y = isCrouching ? baseScaleY * 0.5f : baseScaleY;
        transform.localScale = scale;
    }

    // Toggles the glide mode for the player (glide toggle)
    // Switches to Gliding System and Gliding Camera mode
    private void ToggleGlideMode()
    {
        bool currentlyGliding = glidingSystem != null && glidingSystem.IsGliding;
        bool nextGlideState = !currentlyGliding;

        if (camModeSwitch != null)
        {
            camModeSwitch.SetGliding(nextGlideState);
        }
        else if (glidingSystem != null)
        {
            glidingSystem.SetGliding(nextGlideState);
        }

        jumpQueued = false;
    }


    // For audio clips later
    //--------------------------------
    // private void UpdateFootsteps(bool isMovingOnGround)
    // {
    //     if (footstepSource == null)
    //     {
    //         return;
    //     }
    //
    //     if (!isMovingOnGround)
    //     {
    //         StopFootsteps();
    //         return;
    //     }
    //
    //     AudioClip targetClip = isSprinting && !isCrouching ? sprintClip : walkClip;
    //     if (targetClip == null)
    //     {
    //         return;
    //     }
    //
    //     if (footstepSource.clip != targetClip)
    //     {
    //         footstepSource.clip = targetClip;
    //     }
    //
    //     if (!footstepSource.isPlaying)
    //     {
    //         footstepSource.Play();
    //     }
    // }
    //
    // private void StopFootsteps()
    // {
    //     if (footstepSource != null && footstepSource.isPlaying)
    //     {
    //         footstepSource.Stop();
    //     }
    // }
    //
    // private void UpdateAnimation()
    // {
    //     if (myAnimator == null)
    //     {
    //         return;
    //     }
    //
    //     myAnimator.SetBool("Walking", isWalking);
    //     myAnimator.SetBool("Running", isSprinting && !isCrouching && isWalking);
    // }
}