using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject myCamera;
    [SerializeField] private Transform groundCheck;
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

    private CharacterController myController;
    // private Animator myAnimator;
    private Vector3 velocity;

    private bool isSprinting;
    private bool isWalking;
    private bool isGrounded;
    private bool isCrouching;
    private bool gamePaused;
    private float baseScaleY;

    private void Start()
    {
        myController = GetComponent<CharacterController>();
        // myAnimator = GetComponent<Animator>();
        baseScaleY = transform.localScale.y;
    }

    private void Update()
    {
        if (gamePaused)
        {
            // StopFootsteps();
            return;
        }

        Gravity();
        HandleStateInput();
        MovementInput();
        HandleJumpInput();
        // UpdateAnimation();
    }

    private void HandleStateInput()
    {
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        bool shouldCrouch = Input.GetKey(KeyCode.LeftControl);
        if (shouldCrouch != isCrouching)
        {
            Crouch(shouldCrouch);
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void Gravity()
    {
        isGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = 0f;
        }

        velocity.y += gravity * Time.deltaTime;
        myController.Move(velocity * Time.deltaTime);
    }

    private void MovementInput()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

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

        myController.Move(move * (currentSpeed * speedMulti * Time.deltaTime));
        // UpdateFootsteps(move.sqrMagnitude > 0.01f && isGrounded);
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