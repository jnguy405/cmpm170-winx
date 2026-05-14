using UnityEngine;

public partial class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject myCamera;
    [SerializeField] private Transform groundCheck; 
    [SerializeField] private Transform modelRoot;
    [SerializeField] private GlidingSystem glidingSystem;
    [SerializeField] private CamModeSwitch camModeSwitch;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider physicsCollider;
    [SerializeField] private Animator myAnimator;
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

    [Header("Dash")]
    [SerializeField] private float dashLungeImpulse = 14f;
    [SerializeField] private float dashAnimatorHoldDuration = 0.42f;

    [Header("Grounding")]
    private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

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
    private bool dashPressedThisFrame;
    private bool dashLungePending;
    private Vector3 pendingDashLungeDir;
    private float dashAnimTimer;
    private bool attack1Queued;
    private bool attack2Queued;
    private bool skill1Queued;
    private bool skill2Queued;
    private bool skill3Queued;
    private bool isGrounded;
    private bool wasGrounded;
    private bool wasGliding;
    private bool hasSnappedAfterGlide;
    private bool isCrouching;

    // Game State
    private bool gamePaused;
    private float baseScaleY;
    private Quaternion modelRootBaseLocalRotation;
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
        modelRootBaseLocalRotation = modelRoot.localRotation;

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

        if (myAnimator == null)
        {
            myAnimator = GetComponent<Animator>()
                ?? GetComponentInChildren<Animator>()
                ?? GetComponentInParent<Animator>();
        }

        baseScaleY = transform.localScale.y;

        isGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundDistance, groundMask); // checks if the player is grounded
        wasGrounded = isGrounded;                                                                                  // the player was grounded last frame
        wasGliding = glidingSystem != null && glidingSystem.IsGliding;
        hasSnappedAfterGlide = !wasGliding;
    }

    private void Update()
    {
        if (gamePaused)
        {
            // StopFootsteps();
            return;
        }

        HandleStateInput();
        HandleJumpInput();
        HandleMouseLookInput();
        MovementInput();
        UpdateGroundedState();
        UpdateAnimation();
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
            pendingYawDelta = 0f;
            return;
        }

        ApplyPendingYawRotation();
        float verticalVelocity = ResolveVerticalVelocity();
        ApplyHorizontalMotion();
        ApplyDashLungeIfPending();
        VerticalVelocity(verticalVelocity);
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
        bool currentlyGliding = glidingSystem != null && glidingSystem.IsGliding;
        bool glideEntered = !wasGliding && currentlyGliding;
        if (glideEntered)
        {
            hasSnappedAfterGlide = false;
        }
        bool sphereGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        bool collisionGrounded = IsGroundedByCollisionProbe();
        isGrounded = sphereGrounded || collisionGrounded;

        if (isGrounded && !wasGrounded)
        {
            // Landing should always return from gliding to third-person mode.
            if (currentlyGliding && glidingSystem != null)
            {
                glidingSystem.SetGliding(false);
                currentlyGliding = false;
            }

            if (camModeSwitch != null)
            {
                camModeSwitch.SetGliding(false);
            }
        }

        bool glideExited = wasGliding && !currentlyGliding;
        if (glideExited && camModeSwitch != null)
        {
            camModeSwitch.SetGliding(false);
        }
        else if (camModeSwitch != null && glidingSystem != null
            && !glidingSystem.IsGliding && camModeSwitch.IsGlideCameraActive)
        {
            camModeSwitch.SetGliding(false);
        }

        bool justLanded = isGrounded && !wasGrounded;
        bool shouldSnapAfterGroundedGlideExit = isGrounded && !currentlyGliding && !hasSnappedAfterGlide;
        if (glideExited || (justLanded && wasGliding) || shouldSnapAfterGroundedGlideExit)
        {
            SnapToGroundAndResetPose();
            hasSnappedAfterGlide = true;
        }

        wasGliding = currentlyGliding;
        wasGrounded = isGrounded;
    }

    private void ResetUprightAndFacing()
    {
        // Keep body/camera yaw continuity; only reset visual mesh orientation after glide.
        if (modelRoot != null && modelRoot != transform)
        {
            modelRoot.localRotation = modelRootBaseLocalRotation;
        }
        pendingYawDelta = 0f;
    }

    private void ResetGroundedViewAndFacing()
    {
        if (rb == null)
        {
            return;
        }

        if (camModeSwitch != null)
        {
            camModeSwitch.SetGliding(false);
        }
        else if (glidingSystem != null)
        {
            glidingSystem.SetGliding(false);
        }

        SnapToGroundAndResetPose();
    }

    private void SnapToGroundAndResetPose()
    {
        if (TryGetGroundSnapPosition(out Vector3 snappedPosition))
        {
            rb.position = snappedPosition;
        }

        // Reset to player-forward as source of truth so third-person offset is truly behind the character.
        float targetYaw = rb.rotation.eulerAngles.y;
        rb.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        rb.angularVelocity = Vector3.zero;

        ResetUprightAndFacing();
        if (thirdPersonCam != null)
        {
            thirdPersonCam.ResetToDefaultAtYaw(targetYaw);
        }

        isGrounded = true;
        wasGrounded = true;
        jumpQueued = false;
    }

    private bool TryGetGroundSnapPosition(out Vector3 snappedPosition)
    {
        snappedPosition = rb.position;
        if (physicsCollider == null)
        {
            return false;
        }

        Bounds bounds = physicsCollider.bounds;
        float castHeight = Mathf.Max(1f, bounds.extents.y + 0.5f);
        Vector3 castOrigin = bounds.center + Vector3.up * castHeight;
        float castDistance = castHeight + Mathf.Max(0.5f, groundDistance + 1f);
        float castRadius = Mathf.Max(0.05f, bounds.extents.x * 0.6f);

        if (!Physics.SphereCast(
                castOrigin,
                castRadius,
                Vector3.down,
                out RaycastHit hit,
                castDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        float colliderBottomOffset = bounds.center.y - bounds.min.y;
        snappedPosition = new Vector3(rb.position.x, hit.point.y + colliderBottomOffset, rb.position.z);
        return true;
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

    private void ApplyDashLungeIfPending()
    {
        if (!dashLungePending || rb == null)
        {
            return;
        }

        dashLungePending = false;
        Vector3 dir = pendingDashLungeDir;
        if (dir.sqrMagnitude < 1e-6f)
        {
            return;
        }

        Vector3 v = rb.linearVelocity;
        Vector3 planar = new Vector3(v.x, 0f, v.z) + dir * dashLungeImpulse;
        rb.linearVelocity = new Vector3(planar.x, v.y, planar.z);
    }

    private Vector3 ComputePlanarDashDirection()
    {
        Vector3 fromInput = transform.right * moveX + transform.forward * moveZ;
        fromInput.y = 0f;
        if (fromInput.sqrMagnitude > 0.0001f)
        {
            return fromInput.normalized;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.zero;
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
        bool currentlyGliding = camModeSwitch != null
            ? camModeSwitch.IsGlideCameraActive
            : glidingSystem != null && glidingSystem.IsGliding;
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
        pendingYawDelta = 0f;
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
    private void UpdateAnimation()
    {
        if (myAnimator == null)
        {
            return;
        }

        bool canDash =
            isGrounded
            && !isCrouching
            && (glidingSystem == null || !glidingSystem.IsGliding);

        if (dashPressedThisFrame && dashAnimTimer <= 0f && canDash)
        {
            dashAnimTimer = Mathf.Max(0.01f, dashAnimatorHoldDuration);
            pendingDashLungeDir = ComputePlanarDashDirection();
            dashLungePending = true;
        }

        dashAnimTimer = Mathf.Max(0f, dashAnimTimer - Time.deltaTime);
        bool dashAnimActive = dashAnimTimer > 0f;

        bool isRunningAnim = isSprinting && !isCrouching && isWalking && !dashAnimActive;
        bool isWalkingAnim = isWalking && !isRunningAnim && !dashAnimActive;

        myAnimator.SetBool("Running", isRunningAnim);
        myAnimator.SetBool("Walking", isWalkingAnim);

        myAnimator.SetBool("Dash", dashAnimActive);

        if (attack1Queued)
        {
            myAnimator.SetTrigger("Attack1");
        }

        if (attack2Queued)
        {
            myAnimator.SetTrigger("Attack2");
        }

        if (skill1Queued)
        {
            myAnimator.SetTrigger("Skill1");
        }

        if (skill2Queued)
        {
            myAnimator.SetTrigger("Skill2");
        }

        if (skill3Queued)
        {
            myAnimator.SetTrigger("Skill3");
        }

        attack1Queued = false;
        attack2Queued = false;
        skill1Queued = false;
        skill2Queued = false;
        skill3Queued = false;
    }
}