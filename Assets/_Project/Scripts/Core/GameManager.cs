using MathRunner.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathRunner.Core
{
    // Owns the game state and is the only writer of Time.timeScale: pause (state) combined with the Focus Zone slow-down.
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private InputReader _input;

        private float _baseFixedDelta;

        public GameState State { get; private set; }

        private void Awake()
        {
            _baseFixedDelta = Time.fixedDeltaTime;
        }

        private void OnEnable()
        {
            _input.PausePressed += TogglePause;
            GameEvents.OutOfLives += OnOutOfLives;
            GameEvents.LevelCompleted += OnLevelCompleted;
            GameEvents.FocusStarted += OnFocusStarted;
            GameEvents.FocusEnded += OnFocusEnded;
        }

        private void OnDisable()
        {
            _input.PausePressed -= TogglePause;
            GameEvents.OutOfLives -= OnOutOfLives;
            GameEvents.LevelCompleted -= OnLevelCompleted;
            GameEvents.FocusStarted -= OnFocusStarted;
            GameEvents.FocusEnded -= OnFocusEnded;
        }

        private void Start()
        {
            SetState(GameState.Playing);
        }

        private void OnDestroy()
        {
            ResetTime();
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void Restart()
        {
            ResetTime();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnOutOfLives()
        {
            if (State == GameState.Playing) SetState(GameState.GameOver);
        }

        private void OnLevelCompleted()
        {
            if (State == GameState.Playing) SetState(GameState.LevelComplete);
        }

        private void OnFocusStarted(float factor)
        {
            WorldTime.FocusFactor = Mathf.Clamp(factor, 0.01f, 1f);
            ApplyTimeScale();
        }

        private void OnFocusEnded()
        {
            WorldTime.FocusFactor = 1f;
            ApplyTimeScale();
        }

        private void SetState(GameState state)
        {
            State = state;
            ApplyTimeScale();
            _input.ControlsEnabled = state == GameState.Playing;
            GameEvents.RaiseGameStateChanged(state);
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = WorldTime.Combine(State != GameState.Playing, WorldTime.FocusFactor);
            // Smaller physics steps in slow-mo keep movement smooth instead of stepping 5× less often.
            Time.fixedDeltaTime = _baseFixedDelta * WorldTime.FocusFactor;
        }

        // Leaving or reloading the scene mid-focus must never carry slow-motion into the next scene.
        private void ResetTime()
        {
            WorldTime.FocusFactor = 1f;
            Time.timeScale = 1f;
            if (_baseFixedDelta > 0f) Time.fixedDeltaTime = _baseFixedDelta;
        }
    }
}
