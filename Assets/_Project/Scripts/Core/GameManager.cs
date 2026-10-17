using MathRunner.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathRunner.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private InputReader _input;

        public GameState State { get; private set; }

        private void OnEnable()
        {
            _input.PausePressed += TogglePause;
            GameEvents.OutOfLives += OnOutOfLives;
            GameEvents.LevelCompleted += OnLevelCompleted;
        }

        private void OnDisable()
        {
            _input.PausePressed -= TogglePause;
            GameEvents.OutOfLives -= OnOutOfLives;
            GameEvents.LevelCompleted -= OnLevelCompleted;
        }

        private void Start()
        {
            SetState(GameState.Playing);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
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

        private void SetState(GameState state)
        {
            State = state;
            Time.timeScale = state == GameState.Playing ? 1f : 0f;
            _input.ControlsEnabled = state == GameState.Playing;
            GameEvents.RaiseGameStateChanged(state);
        }
    }
}
