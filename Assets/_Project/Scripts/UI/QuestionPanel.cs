using MathRunner.Core;
using MathRunner.Math;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    public class QuestionPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _questionText;
        [SerializeField] private GameObject _timerBar;
        [SerializeField] private RectTransform _timerFill;
        [SerializeField] private Image _timerFillImage;
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private float _resultDisplaySeconds = 1.5f;

        [Header("Animation")]
        [SerializeField] private float _popInSeconds = 0.25f;
        [Tooltip("The timer bar pulses during this many final seconds.")]
        [SerializeField] private float _pulseSeconds = 1f;
        [SerializeField] private float _pulseScale = 0.12f;
        [SerializeField] private float _pulsesPerSecond = 4f;

        [Header("Colors")]
        [SerializeField] private Color _timerColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color _timerUrgentColor = new Color(0.95f, 0.35f, 0.3f);
        [SerializeField] private Color _correctColor = new Color(0.4f, 0.95f, 0.5f);
        [SerializeField] private Color _wrongColor = new Color(1f, 0.45f, 0.4f);

        private float _timeLimit;
        private float _timeLeft;
        private bool _counting;
        private float _hideAt = -1f;
        private Coroutine _popIn;

        private void Awake()
        {
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.QuestionShown += OnQuestionShown;
            GameEvents.AnswerLocked += OnAnswerLocked;
            GameEvents.QuestionResolved += OnQuestionResolved;
        }

        private void OnDisable()
        {
            GameEvents.QuestionShown -= OnQuestionShown;
            GameEvents.AnswerLocked -= OnAnswerLocked;
            GameEvents.QuestionResolved -= OnQuestionResolved;
        }

        private void OnQuestionShown(MathQuestion question, float timeLimit)
        {
            _panel.SetActive(true);
            _questionText.text = question.Text;
            _questionText.color = Color.white;
            _feedbackText.gameObject.SetActive(false);
            _timeLimit = timeLimit;
            _timeLeft = timeLimit;
            _counting = timeLimit > 0f;
            _timerBar.SetActive(_counting);
            SetFill(1f);
            _hideAt = -1f;
            PopIn();
        }

        // The countdown stops at the moment of the pick, so the bar shows how quickly the player answered.
        private void OnAnswerLocked(int choiceIndex)
        {
            _counting = false;
            _timerBar.transform.localScale = Vector3.one;
        }

        private void OnQuestionResolved(QuestionResult result)
        {
            _counting = false;
            _timerBar.transform.localScale = Vector3.one;
            _timerBar.SetActive(false);
            var q = result.Question;
            _questionText.text = q.Text.Replace("?", q.Answer.ToString());

            bool correct = result.Outcome == AnswerOutcome.Correct;
            _questionText.color = correct ? _correctColor : Color.white;
            _feedbackText.gameObject.SetActive(true);
            _feedbackText.color = correct ? _correctColor : _wrongColor;
            _feedbackText.text = result.Outcome switch
            {
                AnswerOutcome.Correct => "Correct!",
                AnswerOutcome.Wrong => "Not quite! The answer is " + q.Answer,
                _ => (result.TimeLimit > 0f ? "Time's up! The answer is " : "Missed! The answer is ") + q.Answer
            };
            _hideAt = Time.time + _resultDisplaySeconds;
        }

        private void Update()
        {
            if (_counting)
            {
                _timeLeft = Mathf.Max(0f, _timeLeft - Time.deltaTime);
                SetFill(_timeLeft / _timeLimit);
                PulseTimer();
            }
            if (_hideAt >= 0f && Time.time >= _hideAt)
            {
                _panel.SetActive(false);
                _hideAt = -1f;
            }
        }

        private void PopIn()
        {
            if (_popIn != null) StopCoroutine(_popIn);
            var rect = _panel.transform;
            _popIn = StartCoroutine(Tween.Run(_popInSeconds, Ease.OutBack,
                k => rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, k), () => _popIn = null));
        }

        // Throbs the timer bar during the final second so running out of time is felt, not just seen.
        private void PulseTimer()
        {
            float s = 1f;
            if (_timeLeft > 0f && _timeLeft <= _pulseSeconds)
            {
                float phase = (_pulseSeconds - _timeLeft) * _pulsesPerSecond;
                s += _pulseScale * Mathf.Abs(Mathf.Sin(phase * Mathf.PI));
            }
            _timerBar.transform.localScale = new Vector3(1f, s, 1f);
        }

        private void SetFill(float fraction)
        {
            _timerFill.anchorMax = new Vector2(fraction, 1f);
            _timerFillImage.color = fraction < 0.3f ? _timerUrgentColor : _timerColor;
        }
    }
}
