using MathRunner.Core;
using MathRunner.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    // Pause, Game Over and Level Complete screens, plus the on-screen pause button.
    public class OverlayController : MonoBehaviour
    {
        [SerializeField] private GameManager _game;
        [SerializeField] private ScoreSystem _score;
        [SerializeField] private LivesSystem _lives;

        [Header("Panels")]
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _levelCompletePanel;
        [SerializeField] private TextMeshProUGUI _gameOverStats;
        [SerializeField] private TextMeshProUGUI _levelCompleteStats;

        [Header("Buttons")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _pauseRestartButton;
        [SerializeField] private Button _gameOverRetryButton;
        [SerializeField] private Button _levelCompleteRetryButton;

        private void Awake()
        {
            _pauseButton.onClick.AddListener(_game.TogglePause);
            _resumeButton.onClick.AddListener(_game.TogglePause);
            _pauseRestartButton.onClick.AddListener(_game.Restart);
            _gameOverRetryButton.onClick.AddListener(_game.Restart);
            _levelCompleteRetryButton.onClick.AddListener(_game.Restart);
            Show(GameState.Playing);
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += Show;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= Show;
        }

        private void Show(GameState state)
        {
            _pauseButton.gameObject.SetActive(state == GameState.Playing);
            _pausePanel.SetActive(state == GameState.Paused);
            _gameOverPanel.SetActive(state == GameState.GameOver);
            _levelCompletePanel.SetActive(state == GameState.LevelComplete);

            string stats = $"Score: {_score.Score}\nCorrect answers: {_score.CorrectCount} / {_score.AnsweredCount}\nBest streak: {_score.BestStreak}";
            if (state == GameState.GameOver) _gameOverStats.text = stats;
            if (state == GameState.LevelComplete) _levelCompleteStats.text = stats + $"\nLives left: {_lives.Lives}";
        }
    }
}
