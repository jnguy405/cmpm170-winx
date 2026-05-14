using UnityEngine;
using UnityEngine.InputSystem;

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(CreatureMover))]
    public class MovePlayerInput : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField]
        private KeyCode m_RunKey = KeyCode.LeftShift;

        [Header("Camera")]
        [SerializeField]
        private PlayerCamera m_Camera;

        private CreatureMover m_Mover;

        private Vector2 m_Axis;
        private bool m_IsRun;
        private bool m_IsJump;

        private Vector3 m_Target;
        private Vector2 m_MouseDelta;
        private float m_Scroll;

        private void Awake()
        {
            m_Mover = GetComponent<CreatureMover>();
        }

        private void Update()
        {
            GatherInput();
            SetInput();
        }

        public void GatherInput()
        {
            m_Axis = ReadMoveAxis();
            m_IsRun = IsKeyHeld(m_RunKey);
            m_IsJump = IsJumpHeld();

            m_Target = (m_Camera == null) ? Vector3.zero : m_Camera.Target;

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                m_MouseDelta = Vector2.zero;
                m_Scroll = 0f;
            }
            else
            {
                m_MouseDelta = mouse.delta.ReadValue();
                m_Scroll = mouse.scroll.ReadValue().y;
            }
        }

        public void BindMover(CreatureMover mover)
        {
            m_Mover = mover;
        }

        public void SetInput()
        {
            if (m_Mover != null)
            {
                m_Mover.SetInput(in m_Axis, in m_Target, in m_IsRun, m_IsJump);
            }

            if (m_Camera != null)
            {
                m_Camera.SetInput(in m_MouseDelta, m_Scroll);
            }
        }

        static Vector2 ReadMoveAxis()
        {
            float x = AxisFromKeyboard(Key.D, Key.A, Key.RightArrow, Key.LeftArrow);
            float y = AxisFromKeyboard(Key.W, Key.S, Key.UpArrow, Key.DownArrow);
            Vector2 keyboardAxis = new Vector2(x, y);
            if (keyboardAxis.sqrMagnitude > 0.01f)
                return keyboardAxis;

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
                return Vector2.zero;

            Vector2 stick = gamepad.leftStick.ReadValue();
            return stick.sqrMagnitude > 0.01f ? stick : Vector2.zero;
        }

        static float AxisFromKeyboard(Key positive, Key negative, Key positiveAlt, Key negativeAlt)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return 0f;

            bool positivePressed = keyboard[positive].isPressed || keyboard[positiveAlt].isPressed;
            bool negativePressed = keyboard[negative].isPressed || keyboard[negativeAlt].isPressed;

            if (positivePressed == negativePressed)
                return 0f;

            return positivePressed ? 1f : -1f;
        }

        static bool IsJumpHeld()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.isPressed)
                return true;

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.isPressed;
        }

        static bool IsKeyHeld(KeyCode keyCode)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            return keyCode switch
            {
                KeyCode.LeftShift => keyboard.leftShiftKey.isPressed,
                KeyCode.RightShift => keyboard.rightShiftKey.isPressed,
                KeyCode.LeftControl => keyboard.leftCtrlKey.isPressed,
                KeyCode.RightControl => keyboard.rightCtrlKey.isPressed,
                KeyCode.LeftAlt => keyboard.leftAltKey.isPressed,
                KeyCode.RightAlt => keyboard.rightAltKey.isPressed,
                KeyCode.Space => keyboard.spaceKey.isPressed,
                _ => false,
            };
        }
    }
}
