using System;
using System.Collections.Generic;
using MathRunner.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MathRunner.Input
{
    // Jump: Space/W/Up, or a click/tap anywhere that isn't on a UI button. Answer: 1/2/3 (or the answer buttons). Pause: Esc/P.
    public class InputReader : MonoBehaviour
    {
        private const int AnswerKeyCount = 3;

        public event Action JumpPressed;
        public event Action JumpReleased;
        public event Action PausePressed;

        public bool JumpHeld { get; private set; }

        // Off while paused or after the level ends: jumps and answers are ignored.
        public bool ControlsEnabled
        {
            get => _controlsEnabled;
            set
            {
                _controlsEnabled = value;
                if (!value && JumpHeld) NotifyJumpReleased();
            }
        }

        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private readonly InputAction[] _answerKeys = new InputAction[AnswerKeyCount];
        private readonly Action<InputAction.CallbackContext>[] _answerHandlers = new Action<InputAction.CallbackContext>[AnswerKeyCount];
        private InputAction _keyboardJump;
        private InputAction _pause;
        private bool _controlsEnabled = true;
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

            for (int i = 0; i < AnswerKeyCount; i++)
            {
                int index = i;
                _answerKeys[i] = new InputAction("Answer" + (i + 1), InputActionType.Button);
                _answerKeys[i].AddBinding("<Keyboard>/" + (i + 1));
                _answerKeys[i].AddBinding("<Keyboard>/numpad" + (i + 1));
                _answerHandlers[i] = _ => NotifyAnswerChosen(index);
            }
        }

        private void OnEnable()
        {
            _keyboardJump.started += OnJumpStarted;
            _keyboardJump.canceled += OnJumpCanceled;
            _pause.performed += OnPausePerformed;
            _keyboardJump.Enable();
            _pause.Enable();
            for (int i = 0; i < AnswerKeyCount; i++)
            {
                _answerKeys[i].performed += _answerHandlers[i];
                _answerKeys[i].Enable();
            }
        }

        private void OnDisable()
        {
            _keyboardJump.started -= OnJumpStarted;
            _keyboardJump.canceled -= OnJumpCanceled;
            _pause.performed -= OnPausePerformed;
            _keyboardJump.Disable();
            _pause.Disable();
            for (int i = 0; i < AnswerKeyCount; i++)
            {
                _answerKeys[i].performed -= _answerHandlers[i];
                _answerKeys[i].Disable();
            }
            JumpHeld = false;
            _pointerJumping = false;
        }

        private void OnDestroy()
        {
            _keyboardJump.Dispose();
            _pause.Dispose();
            foreach (var a in _answerKeys) a.Dispose();
        }

        public void NotifyAnswerChosen(int choiceIndex)
        {
            if (!_controlsEnabled) return;
            GameEvents.RaiseAnswerChosen(choiceIndex);
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
            if (!_controlsEnabled) return;
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
