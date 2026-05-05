using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject myCamera;
    [SerializeField] private Transform groundCheck; 
    [SerializeField] private GlidingSystem glidingSystem;
    [SerializeField] private CamModeSwitch camModeSwitch;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider physicsCollider;
    // [SerializeField] private AudioSource footstepSource;
    // [SerializeField] private AudioClip walkClip;
    // [SerializeField] private AudioClip sprintClip;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float speedMulti = 1f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Grounding")]
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    // private Animator myAnimator;
    private float moveX;
    private float moveZ;
    private bool jumpQueued;

    private bool isSprinting;
    private bool isWalking;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isCrouching;
    private bool gamePaused;
    private float baseScaleY;
    private const float groundedVerticalClamp = 0f;

    private void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>()
                ?? GetComponentInParent<Rigidbody>()
                ?? GetComponentInChildren<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogError($"{nameof(PlayerController)} on '{name}': no Rigidbody assigned or found. Assign the shared player Rigidbody in the inspector.", this);
            enabled = false;
            return;
        }

        if (physicsCollider == null)
        {
            physicsCollider = GetComponent<Collider>()
                ?? rb.GetComponent<Collider>()
                ?? rb.GetComponentInChildren<Collider>();
        }

        if (physicsCollider == null)
        {
            Debug.LogError($"{nameof(PlayerController)} on '{name}': no Collider assigned or found for grounding probe.", this);
            enabled = false;
            return;
        }

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = true;
        rb.isKinematic = false;

        // myAnimator = GetComponent<Animator>();
        baseScaleY = transform.localScale.y;

        if (glidingSystem == null)
        {
            glidingSystem = FindAnyObjectByType<GlidingSystem>();
        }

        if (camModeSwitch == null)
        {
            camModeSwitch = FindAnyObjectByType<CamModeSwitch>();
        }

        isGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        wasGrounded = isGrounded;
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
        MovementInput();
        HandleJumpInput();
        // UpdateAnimation();
    }

    private void HandleStateInput()
    {
        Keyboard keyboard = Keyboard.current;
        isSprinting = keyboard != null && keyboard.leftShiftKey.isPressed;

        bool shouldCrouch = keyboard != null && keyboard.leftCtrlKey.isPressed;
        if (shouldCrouch != isCrouching)
        {
            Crouch(shouldCrouch);
        }
    }

    private void HandleJumpInput()
    {
        if (glidingSystem != null && glidingSystem.IsGliding)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        bool jumpPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        if (jumpPressed && isGrounded)
        {
            jumpQueued = true;
        }
    }

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

        float verticalVelocity = ResolveVerticalVelocity();
        ApplyHorizontalMotion();
        VerticalVelocity(verticalVelocity);
    }

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

        Vector3 planarVelocity = move * (currentSpeed * speedMulti);
        rb.linearVelocity = new Vector3(planarVelocity.x, rb.linearVelocity.y, planarVelocity.z);
    }

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

    private void Crouch(bool crouch)
    {
        isCrouching = crouch;

        Vector3 scale = transform.localScale;
        scale.y = isCrouching ? baseScaleY * 0.5f : baseScaleY;
        transform.localScale = scale;
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