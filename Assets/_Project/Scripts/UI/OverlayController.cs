using MathRunner.Core;
using MathRunner.Level;
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
        [SerializeField] private QuestionDirector _director;

        [Header("Panels")]
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _levelCompletePanel;
        [SerializeField] private TextMeshProUGUI _gameOverStats;
        [SerializeField] private TextMeshProUGUI _levelCompleteStats;
        [SerializeField] private TextMeshProUGUI _levelCompleteResult;

        [Header("Buttons")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _pauseRestartButton;
        [SerializeField] private Button _pauseLevelsButton;
        [SerializeField] private Button _gameOverRetryButton;
        [SerializeField] private Button _gameOverLevelsButton;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _levelCompleteRetryButton;
        [SerializeField] private Button _levelCompleteLevelsButton;

        private void Awake()
        {
            _pauseButton.onClick.AddListener(_game.TogglePause);
            _resumeButton.onClick.AddListener(_game.TogglePause);
            _pauseRestartButton.onClick.AddListener(_game.Restart);
            _gameOverRetryButton.onClick.AddListener(_game.Restart);
            _levelCompleteRetryButton.onClick.AddListener(_game.Restart);
            _nextLevelButton.onClick.AddListener(PlayNextLevel);
            foreach (var b in new[] { _pauseLevelsButton, _gameOverLevelsButton, _levelCompleteLevelsButton })
                b.onClick.AddListener(() => SceneFlow.Load(SceneFlow.LevelSelect));
            Show(GameState.Playing);
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += Show;
            GameEvents.ProgressSaved += OnProgressSaved;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= Show;
            GameEvents.ProgressSaved -= OnProgressSaved;
        }

        private LevelConfig NextLevel => LevelCatalog.Load().Next(_director.Config);

        private void PlayNextLevel() => SceneFlow.PlayLevel(NextLevel);

        private void Show(GameState state)
        {
            _pauseButton.gameObject.SetActive(state == GameState.Playing);
            _pausePanel.SetActive(state == GameState.Paused);
            _gameOverPanel.SetActive(state == GameState.GameOver);
            _levelCompletePanel.SetActive(state == GameState.LevelComplete);

            string stats = $"Score: {_score.Score}\nCorrect answers: {_score.CorrectCount} / {_score.AnsweredCount}\nBest streak: {_score.BestStreak}";
            if (state == GameState.GameOver) _gameOverStats.text = stats;
            if (state == GameState.LevelComplete)
            {
                _levelCompleteStats.text = stats + $"\nLives left: {_lives.Lives}";
                _levelCompleteResult.text = "";
                _nextLevelButton.gameObject.SetActive(NextLevel != null);
            }
        }

        private void OnProgressSaved(LevelResult result)
        {
            _levelCompleteResult.text = StarRating.Display(result.Stars) + "\n" + (result.NewBest ? "New best score!" : $"Best: {result.BestScore}");
        }
    }
}
