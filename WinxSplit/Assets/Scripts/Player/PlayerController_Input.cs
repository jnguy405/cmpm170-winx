using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerController
{
    // Handles the state input for the player (glide toggle, sprint, crouch)
    private void HandleStateInput()
    {
        Keyboard keyboard = Keyboard.current;
        bool glideTogglePressed = keyboard != null && keyboard.gKey.wasPressedThisFrame;

        if (glideTogglePressed)
        {
            ToggleGlideMode();
        }

        bool resetViewPressed = keyboard != null && keyboard.vKey.wasPressedThisFrame;
        if (resetViewPressed)
        {
            ResetGroundedViewAndFacing();
        }

        isSprinting = keyboard != null && keyboard.leftCtrlKey.isPressed;

        bool shouldCrouch = keyboard != null && keyboard.leftShiftKey.isPressed;
        if (shouldCrouch != isCrouching)
        {
            Crouch(shouldCrouch);
        }
    }

    // Handles jump input with Space when not gliding
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

    // Handles the mouse look input for the player (yaw rotation)
    private void HandleMouseLookInput()
    {
        if (glidingSystem != null && glidingSystem.IsGliding)
        {
            pendingYawDelta = 0f;
            return;
        }

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
        isWalking = new Vector2(moveX, moveZ).sqrMagnitude > 0.01f;
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
}
