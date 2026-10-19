using System;
using MathRunner.Core;
using MathRunner.Input;
using MathRunner.Math;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    // The answers chalked on the question board, left to right (keys 1–4), each in its own coloured chalk circle.
    // Circles beyond the question's choice count stay hidden. After the pick the chosen circle is drawn thicker and the
    // others fade; once resolved, a tick marks the right answer and a cross a wrong pick (the board then hides them).
    public class AnswerButtons : MonoBehaviour
    {
        private const float ChosenScale = 1.15f;
        private const float FadedAlpha = 0.4f;

        [SerializeField] private InputReader _input;
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _hint;
        [SerializeField] private Button[] _buttons = new Button[4];
        [SerializeField] private TextMeshProUGUI[] _labels = new TextMeshProUGUI[4];
        [SerializeField] private Image[] _rings = new Image[4];
        [SerializeField] private Image[] _marks = new Image[4];

        public const string RouteHint = "Jump onto the right answer!";

        private UiTheme _theme;
        private AnswerMode _mode = AnswerMode.Tap;
        private string _tapHint;
        private Color _tapHintColor;
        private Vector2 _tapHintMin, _tapHintMax, _tapHintAnchorMin, _tapHintAnchorMax;
        private float _tapHintSize;

        private UiTheme Theme => _theme != null ? _theme : _theme = UiTheme.Load();

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
            GameEvents.AnswerModeSet += SetMode;
            GameEvents.QuestionShown += OnQuestionShown;
            GameEvents.AnswerLocked += Lock;
            GameEvents.QuestionResolved += Resolve;
        }

        private void OnDisable()
        {
            GameEvents.AnswerModeSet -= SetMode;
            GameEvents.QuestionShown -= OnQuestionShown;
            GameEvents.AnswerLocked -= Lock;
            GameEvents.QuestionResolved -= Resolve;
        }

        // Tap (enemy encounter): tappable circles numbered 1–4. Route (fork in the path): no circles, the answers are on
        // the route signs and the runner answers by jumping.
        public void SetMode(AnswerMode mode) => _mode = mode;

        // Whether this button is one of the answers (the audio skips their click: they sound through AnswerLocked).
        public bool Owns(Button button) => Array.IndexOf(_buttons, button) >= 0;

        // Alpha of circle `index` once `chosenIndex` is locked (-1 = nothing locked yet).
        public static float RingAlpha(int index, int chosenIndex) => chosenIndex < 0 || index == chosenIndex ? 1f : FadedAlpha;

        private void OnQuestionShown(MathQuestion question, float timeLimit) => Show(question);

        public void Show(MathQuestion question)
        {
            bool route = _mode == AnswerMode.Route;
            _root.SetActive(true);
            _hint.SetActive(true);
            ShowHint(route);
            for (int i = 0; i < _buttons.Length; i++)
            {
                // A fork's answers stand on its glowing route signs, so the board shows no circles then.
                bool used = !route && i < question.Choices.Count;
                _buttons[i].gameObject.SetActive(used);
                _marks[i].gameObject.SetActive(false);
                if (!used) continue;
                _labels[i].text = question.Choices[i].ToString();
                _buttons[i].interactable = true;
                _rings[i].raycastTarget = true;
                _buttons[i].transform.localScale = Vector3.one;
                var slot = (RectTransform)_buttons[i].transform;
                slot.anchorMin = slot.anchorMax = new Vector2((i + 0.5f) / question.Choices.Count, slot.anchorMin.y);
                var key = _buttons[i].transform.Find("Key");
                if (key != null && key.TryGetComponent(out TextMeshProUGUI keyText)) keyText.text = (i + 1).ToString();
                SetRing(i, -1);
            }
        }

        // Tap: a small dark hint under the board. Route: a big chalk line on the board where the circles would be.
        private void ShowHint(bool route)
        {
            var hint = _hint.GetComponent<TextMeshProUGUI>();
            if (hint == null) return;
            var rt = hint.rectTransform;
            if (_tapHint == null)
            {
                _tapHint = hint.text;
                _tapHintColor = hint.color;
                _tapHintMin = rt.offsetMin;
                _tapHintMax = rt.offsetMax;
                _tapHintAnchorMin = rt.anchorMin;
                _tapHintAnchorMax = rt.anchorMax;
                _tapHintSize = hint.fontSizeMax;
            }
            hint.text = route ? RouteHint : _tapHint;
            hint.color = route ? Theme.AnswerColor(2) : _tapHintColor;
            hint.fontSizeMax = route ? 52f : _tapHintSize;
            rt.anchorMin = route ? new Vector2(0f, 0.1f) : _tapHintAnchorMin;
            rt.anchorMax = route ? new Vector2(1f, 0.9f) : _tapHintAnchorMax;
            rt.offsetMin = route ? Vector2.zero : _tapHintMin;
            rt.offsetMax = route ? Vector2.zero : _tapHintMax;
        }

        public void Lock(int choiceIndex)
        {
            _hint.SetActive(false);
            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].interactable = false;
                _rings[i].raycastTarget = false; // a disabled circle would still swallow a tap meant as a jump
                _buttons[i].transform.localScale = Vector3.one * (i == choiceIndex ? ChosenScale : 1f);
                SetRing(i, choiceIndex);
            }
        }

        public void Resolve(QuestionResult result)
        {
            _hint.SetActive(false);
            var q = result.Question;
            var marks = AnswerMarks.For(q.Choices.Count, q.CorrectIndex, result.ChosenIndex, _marks.Length);
            for (int i = 0; i < _marks.Length; i++)
            {
                _buttons[i].interactable = false;
                _rings[i].raycastTarget = false;
                bool show = marks[i] != AnswerMark.None;
                _marks[i].gameObject.SetActive(show);
                if (!show) continue;
                bool tick = marks[i] == AnswerMark.Tick;
                _marks[i].sprite = tick ? Theme.Tick : Theme.Cross;
                _marks[i].color = Theme.AnswerColor(tick ? 1 : 3); // green tick, red cross
            }
        }

        private void SetRing(int index, int chosenIndex)
        {
            var c = Theme.AnswerColor(index);
            c.a = RingAlpha(index, chosenIndex);
            _rings[index].color = c;
            var label = Theme.ChalkWhite;
            label.a = c.a;
            _labels[index].color = label;
        }
    }
}
