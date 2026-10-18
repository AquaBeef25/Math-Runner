using MathRunner.Core;
using MathRunner.Input;
using MathRunner.Math;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    // Big tap targets for the answers, left to right (keys 1–4). Buttons beyond the question's choice count stay hidden.
    public class AnswerButtons : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _hint;
        [SerializeField] private Button[] _buttons = new Button[4];
        [SerializeField] private TextMeshProUGUI[] _labels = new TextMeshProUGUI[4];

        [Header("Colors")]
        [SerializeField] private Color _idleColor = new Color(1f, 0.78f, 0.25f);
        [SerializeField] private Color _chosenColor = new Color(1f, 0.95f, 0.6f);
        [SerializeField] private Color _otherColor = new Color(0.6f, 0.6f, 0.65f, 0.6f);

        private void Awake()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i;
                _buttons[i].onClick.AddListener(() => _input.NotifyAnswerChosen(index));
            }
            _root.SetActive(false);
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
            _root.SetActive(true);
            _hint.SetActive(true);
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool used = i < question.Choices.Count;
                _buttons[i].gameObject.SetActive(used);
                if (!used) continue;
                _labels[i].text = question.Choices[i].ToString();
                _buttons[i].interactable = true;
                _buttons[i].targetGraphic.color = _idleColor;
            }
        }

        private void OnAnswerLocked(int choiceIndex)
        {
            _hint.SetActive(false);
            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].interactable = false;
                _buttons[i].targetGraphic.color = i == choiceIndex ? _chosenColor : _otherColor;
            }
        }

        private void OnQuestionResolved(QuestionResult result) => _root.SetActive(false);
    }
}
