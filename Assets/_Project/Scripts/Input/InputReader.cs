using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRunner.Input
{
    // One jump action for every platform: Space/W/Up on keyboard, click or touch anywhere via <Pointer>/press.
    public class InputReader : MonoBehaviour
    {
        public event Action JumpPressed;
        public event Action JumpReleased;

        public bool JumpHeld { get; private set; }

        private InputAction _jump;

        private void Awake()
        {
            _jump = new InputAction("Jump", InputActionType.Button);
            _jump.AddBinding("<Keyboard>/space");
            _jump.AddBinding("<Keyboard>/w");
            _jump.AddBinding("<Keyboard>/upArrow");
            _jump.AddBinding("<Pointer>/press");
        }

        private void OnEnable()
        {
            _jump.started += OnJumpStarted;
            _jump.canceled += OnJumpCanceled;
            _jump.Enable();
        }

        private void OnDisable()
        {
            _jump.started -= OnJumpStarted;
            _jump.canceled -= OnJumpCanceled;
            _jump.Disable();
            JumpHeld = false;
        }

        private void OnDestroy()
        {
            _jump.Dispose();
        }

        public void NotifyJumpPressed()
        {
            JumpHeld = true;
            JumpPressed?.Invoke();
        }

        public void NotifyJumpReleased()
        {
            JumpHeld = false;
            JumpReleased?.Invoke();
        }

        private void OnJumpStarted(InputAction.CallbackContext ctx) => NotifyJumpPressed();

        private void OnJumpCanceled(InputAction.CallbackContext ctx) => NotifyJumpReleased();
    }
}
