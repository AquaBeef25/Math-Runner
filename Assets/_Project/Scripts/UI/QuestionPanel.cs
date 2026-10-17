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

        [Header("Colors")]
        [SerializeField] private Color _timerColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color _timerUrgentColor = new Color(0.95f, 0.35f, 0.3f);
        [SerializeField] private Color _correctColor = new Color(0.4f, 0.95f, 0.5f);
        [SerializeField] private Color _wrongColor = new Color(1f, 0.45f, 0.4f);

        private float _timeLimit;
        private float _timeLeft;
        private bool _counting;
        private float _hideAt = -1f;

        private void Awake()
        {
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.QuestionShown += OnQuestionShown;
            GameEvents.QuestionResolved += OnQuestionResolved;
        }

        private void OnDisable()
        {
            GameEvents.QuestionShown -= OnQuestionShown;
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
        }

        private void OnQuestionResolved(QuestionResult result)
        {
            _counting = false;
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
                _ => "Missed! The answer is " + q.Answer
            };
            _hideAt = Time.time + _resultDisplaySeconds;
        }

        private void Update()
        {
            if (_counting)
            {
                _timeLeft = Mathf.Max(0f, _timeLeft - Time.deltaTime);
                SetFill(_timeLeft / _timeLimit);
            }
            if (_hideAt >= 0f && Time.time >= _hideAt)
            {
                _panel.SetActive(false);
                _hideAt = -1f;
            }
        }

        private void SetFill(float fraction)
        {
            _timerFill.anchorMax = new Vector2(fraction, 1f);
            _timerFillImage.color = fraction < 0.3f ? _timerUrgentColor : _timerColor;
        }
    }
}
