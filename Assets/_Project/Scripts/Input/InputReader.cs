using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MathRunner.Input
{
    // Jump: Space/W/Up, or a click/tap anywhere that isn't on a UI button. Pause: Esc/P.
    public class InputReader : MonoBehaviour
    {
        public event Action JumpPressed;
        public event Action JumpReleased;
        public event Action PausePressed;

        public bool JumpHeld { get; private set; }

        public bool JumpEnabled
        {
            get => _jumpEnabled;
            set
            {
                _jumpEnabled = value;
                if (!value && JumpHeld) NotifyJumpReleased();
            }
        }

        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private InputAction _keyboardJump;
        private InputAction _pause;
        private bool _jumpEnabled = true;
        private bool _pointerJumping;

        private void Awake()
        {
            _keyboardJump = new InputAction("Jump", InputActionType.Button);
            _keyboardJump.AddBinding("<Keyboard>/space");
            _keyboardJump.AddBinding("<Keyboard>/w");
            _keyboardJump.AddBinding("<Keyboard>/upArrow");

            _pause = new InputAction("Pause", InputActionType.Button);
            _pause.AddBinding("<Keyboard>/escape");
            _pause.AddBinding("<Keyboard>/p");
        }

        private void OnEnable()
        {
            _keyboardJump.started += OnJumpStarted;
            _keyboardJump.canceled += OnJumpCanceled;
            _pause.performed += OnPausePerformed;
            _keyboardJump.Enable();
            _pause.Enable();
        }

        private void OnDisable()
        {
            _keyboardJump.started -= OnJumpStarted;
            _keyboardJump.canceled -= OnJumpCanceled;
            _pause.performed -= OnPausePerformed;
            _keyboardJump.Disable();
            _pause.Disable();
            JumpHeld = false;
            _pointerJumping = false;
        }

        private void OnDestroy()
        {
            _keyboardJump.Dispose();
            _pause.Dispose();
        }

        // Pointer is polled here rather than bound to an action so a tap can be checked against the UI first.
        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame && !IsOverUI(pointer.position.ReadValue()))
            {
                _pointerJumping = true;
                NotifyJumpPressed();
            }
            else if (_pointerJumping && pointer.press.wasReleasedThisFrame)
            {
                _pointerJumping = false;
                NotifyJumpReleased();
            }
        }

        public void NotifyJumpPressed()
        {
            if (!_jumpEnabled) return;
            JumpHeld = true;
            JumpPressed?.Invoke();
        }

        public void NotifyJumpReleased()
        {
            JumpHeld = false;
            JumpReleased?.Invoke();
        }

        private bool IsOverUI(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            _uiHits.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screenPosition }, _uiHits);
            return _uiHits.Count > 0;
        }

        private void OnJumpStarted(InputAction.CallbackContext ctx) => NotifyJumpPressed();

        private void OnJumpCanceled(InputAction.CallbackContext ctx) => NotifyJumpReleased();

        private void OnPausePerformed(InputAction.CallbackContext ctx) => PausePressed?.Invoke();
    }
}
