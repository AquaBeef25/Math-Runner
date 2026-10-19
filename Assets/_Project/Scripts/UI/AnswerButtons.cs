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

        private const string RouteHint = "Jump to answer!";
        private static readonly string[] RouteCaptions = { "jump high", "hop", "stay low" };

        private UiTheme _theme;
        private AnswerMode _mode = AnswerMode.Tap;
        private string _tapHint;

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

        // Tap (enemy encounter): tappable circles numbered 1–4. Route (fork in the path): the circles only show the
        // answers and how to reach each route; taps go through to jump.
        public void SetMode(AnswerMode mode) => _mode = mode;

        public static string RouteCaption(int lane) => lane >= 0 && lane < RouteCaptions.Length ? RouteCaptions[lane] : "";

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
            var hint = _hint.GetComponent<TextMeshProUGUI>();
            if (hint != null)
            {
                if (_tapHint == null) _tapHint = hint.text == RouteHint ? "Tap an answer or press 1-4" : hint.text;
                hint.text = route ? RouteHint : _tapHint;
            }
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool used = i < question.Choices.Count;
                _buttons[i].gameObject.SetActive(used);
                _marks[i].gameObject.SetActive(false);
                if (!used) continue;
                _labels[i].text = question.Choices[i].ToString();
                _buttons[i].interactable = !route;
                _rings[i].raycastTarget = !route;
                _buttons[i].transform.localScale = Vector3.one;
                // Spread the used circles evenly across the board (4 for an encounter, 3 for a fork).
                var slot = (RectTransform)_buttons[i].transform;
                slot.anchorMin = slot.anchorMax = new Vector2((i + 0.5f) / question.Choices.Count, slot.anchorMin.y);
                var key = _buttons[i].transform.Find("Key");
                if (key != null && key.TryGetComponent(out TextMeshProUGUI keyText))
                {
                    keyText.text = route ? RouteCaption(i) : (i + 1).ToString();
                    // The caption sits centred above the circle; the key number in its top-left corner.
                    var rt = keyText.rectTransform;
                    rt.anchorMin = rt.anchorMax = route ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
                    rt.pivot = route ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = route ? new Vector2(0f, 6f) : new Vector2(18f, -4f);
                    rt.sizeDelta = route ? new Vector2(180f, 30f) : new Vector2(36f, 32f);
                }
                SetRing(i, -1);
            }
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
