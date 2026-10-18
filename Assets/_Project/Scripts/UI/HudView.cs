using System.Linq;
using MathRunner.Core;
using MathRunner.Systems;
using TMPro;
using UnityEngine;

namespace MathRunner.UI
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private QuestionDirector _director;
        [SerializeField] private TextMeshProUGUI _livesText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _streakText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private string _lifeSymbol = "♥";

        private int _coins;

        private void OnEnable()
        {
            GameEvents.LivesChanged += OnLivesChanged;
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.CoinCollected += OnCoinCollected;
        }

        private void OnDisable()
        {
            GameEvents.LivesChanged -= OnLivesChanged;
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.CoinCollected -= OnCoinCollected;
        }

        private void Start()
        {
            _levelText.text = "Level " + _director.Level;
            if (_coinText != null) _coinText.text = "0";
        }

        private void OnLivesChanged(int lives)
        {
            _livesText.text = string.Join(" ", Enumerable.Repeat(_lifeSymbol, lives));
        }

        private void OnCoinCollected(int value)
        {
            _coins++;
            if (_coinText != null) _coinText.text = _coins.ToString();
        }

        private void OnScoreChanged(ScoreUpdate update)
        {
            _scoreText.text = "Score " + update.Score;
            _streakText.text = update.Streak >= 2 ? update.Streak + " in a row!" : "";
        }
    }
}
