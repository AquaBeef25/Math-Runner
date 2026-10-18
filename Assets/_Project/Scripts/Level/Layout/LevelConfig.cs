using MathRunner.Math;
using UnityEngine;

namespace MathRunner.Level
{
    // Everything that makes one level different: its math, pacing and layout. Seeded from LevelCurriculum, then tuned here.
    [CreateAssetMenu(menuName = "MathRunner/Level Config", fileName = "Level00")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _levelNumber = 1;
        [SerializeField] private string _topic = "Addition";
        [SerializeField] private QuestionSettings _questions = new QuestionSettings();

        [Header("Pacing")]
        [SerializeField, Min(1)] private int _questionCount = 8;
        [SerializeField, Min(1f)] private float _runSpeed = 6f;
        [Tooltip("Share of questions with a visible countdown (0 = none, 1 = all). Spread evenly; the first question is untimed unless this is 1.")]
        [SerializeField, Range(0f, 1f)] private float _timedRatio;
        [Tooltip("Seconds to answer a timed question (= run-up time to the platforms).")]
        [SerializeField, Min(0f)] private float _timeLimit;
        [Tooltip("Run-up seconds for untimed questions. Never shorter than the time limit.")]
        [SerializeField, Min(1f)] private float _untimedLeadSeconds = 6f;

        [Header("Layout")]
        [SerializeField, Min(0)] private int _hazardsBetweenQuestions = 1;
        [Tooltip("Chance that a hazard is a gap instead of a spike.")]
        [SerializeField, Range(0f, 1f)] private float _gapChance;
        [SerializeField] private int _layoutSeed = 1;

        [Header("World shape")]
        [Tooltip("0 = flat ground; 1–2 = steps and pillars up to that many tiles.")]
        [SerializeField, Range(0, 2)] private int _maxStepTiles = 1;
        [Tooltip("High/low route splits per level.")]
        [SerializeField, Min(0)] private int _routeSplits = 1;
        [Tooltip("Chance a spike gets an optional platform over it.")]
        [SerializeField, Range(0f, 1f)] private float _platformChance = 0.3f;

        [Header("Look")]
        [SerializeField] private BackgroundTheme _background;

        public int LevelNumber => _levelNumber;
        public string Topic => _topic;
        public QuestionSettings Questions => _questions;
        public int QuestionCount => _questionCount;
        public float RunSpeed => _runSpeed;
        public float TimedRatio => _timedRatio;
        public float TimeLimit => _timeLimit;
        public float UntimedLeadSeconds => Mathf.Max(_untimedLeadSeconds, _timeLimit);
        public int HazardsBetweenQuestions => _hazardsBetweenQuestions;
        public float GapChance => _gapChance;
        public int LayoutSeed => _layoutSeed;
        public int MaxStepTiles => _maxStepTiles;
        public int RouteSplits => _routeSplits;
        public float PlatformChance => _platformChance;
        public BackgroundTheme Background => _background;

        public void Initialize(int levelNumber, string topic, QuestionSettings questions, int questionCount, float runSpeed,
            float timedRatio, float timeLimit, int hazardsBetweenQuestions, float gapChance)
        {
            _levelNumber = levelNumber;
            _topic = topic;
            _questions = questions;
            _questionCount = questionCount;
            _runSpeed = runSpeed;
            _timedRatio = timedRatio;
            _timeLimit = timeLimit;
            _hazardsBetweenQuestions = hazardsBetweenQuestions;
            _gapChance = gapChance;
            _layoutSeed = levelNumber;
        }

        private void OnValidate()
        {
            var errors = _questions.Validate();
            if (errors.Count > 0) Debug.LogWarning($"{name}: {string.Join(" ", errors)}", this);
            if (_timedRatio > 0f && _timeLimit <= 0f) Debug.LogWarning($"{name}: timed questions need a time limit above 0.", this);
        }
    }
}
