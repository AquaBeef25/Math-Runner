using MathRunner.Core;
using MathRunner.Math;
using MathRunner.Player;
using MathRunner.Systems;
using UnityEngine;

namespace MathRunner.Level
{
    // trigger ──(time limit × speed)──► decision line ──(landing air time × speed)──► three answer platforms
    // The trigger shows the question. The player picks an answer (button or 1/2/3) before the decision line; the runner
    // then jumps onto that platform by itself. No pick by the decision line counts as a miss.
    [RequireComponent(typeof(BoxCollider2D))]
    public class QuestionZone : MonoBehaviour
    {
        [SerializeField] private QuestionDirector _director;
        [SerializeField] private AnswerPlatform[] _platforms = new AnswerPlatform[3];

        [Header("Timing")]
        [Tooltip("Seconds to pick an answer after the question appears (= run-up to the decision line). 0 = untimed.")]
        [SerializeField, Min(0f)] private float _timeLimit;
        [Tooltip("Run-up time to the decision line when the question is untimed.")]
        [SerializeField, Min(1f)] private float _untimedLeadTime = 4f;
        [Tooltip("Run speed used to place the platforms. The level builder sets this from the level's run speed.")]
        [SerializeField, Min(0.1f)] private float _layoutRunSpeed = 6f;
        [Tooltip("Seconds from take-off to landing on a platform. The level builder computes this from the player's jump.")]
        [SerializeField, Min(0.1f)] private float _landingAirTime = 0.65f;

        [Header("Layout")]
        [Tooltip("Seconds of running across one platform. Width = run speed × this.")]
        [SerializeField, Min(0.1f)] private float _platformSeconds = 0.5f;
        [Tooltip("Seconds of running across the gap between platforms.")]
        [SerializeField, Min(0f)] private float _platformGapSeconds = 0.25f;
        [Tooltip("Height of the platform surface above the ground. Must leave room for the player to run underneath.")]
        [SerializeField] private float _platformTop = 1.4f;
        [SerializeField] private float _platformThickness = 0.3f;

        private MathQuestion _question;
        private float _shownTimeLimit;
        private float _shownAt;
        private float _remainingFraction;
        private int _chosenIndex = -1;
        private PlayerController _player;
        private bool _active;
        private bool _resolved;

        public float LeadDistance => (_timeLimit > 0f ? _timeLimit : _untimedLeadTime) * _layoutRunSpeed;
        public float DecisionX => transform.position.x + LeadDistance;
        public float PlatformTop => _platformTop;

        // Seconds of running from the first platform's left edge to the last platform's right edge.
        public float PlatformSpanSeconds => _platformSeconds * _platforms.Length + _platformGapSeconds * (_platforms.Length - 1);

        public float Length => LeadDistance + (_landingAirTime + PlatformSpanSeconds) * _layoutRunSpeed;

        private float PlatformWidth => _platformSeconds * _layoutRunSpeed;
        private float PlatformGap => _platformGapSeconds * _layoutRunSpeed;
        private float EndX => transform.position.x + Length;

        public void SetDirector(QuestionDirector director) => _director = director;

        public void Configure(float runSpeed, float timeLimit, float untimedLeadTime, float landingAirTime)
        {
            _layoutRunSpeed = runSpeed;
            _timeLimit = timeLimit;
            _untimedLeadTime = untimedLeadTime;
            _landingAirTime = landingAirTime;
            ApplyLayout();
        }

        private void Awake()
        {
            for (int i = 0; i < _platforms.Length; i++) _platforms[i].Bind(this, i);
            ApplyLayout();
        }

        private void OnValidate()
        {
            if (_platforms != null) ApplyLayout();
        }

        private void OnDisable()
        {
            GameEvents.AnswerChosen -= OnAnswerChosen;
        }

        private void ApplyLayout()
        {
            float firstLeft = LeadDistance + _landingAirTime * _layoutRunSpeed;
            for (int i = 0; i < _platforms.Length; i++)
            {
                if (_platforms[i] == null) continue;
                float x = firstLeft + PlatformWidth * 0.5f + i * (PlatformWidth + PlatformGap);
                var center = new Vector2(x, _platformTop - _platformThickness * 0.5f);
                _platforms[i].SetLayout(center, new Vector2(PlatformWidth, _platformThickness));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_active || _resolved || !other.TryGetComponent(out PlayerController player)) return;

            _player = player;
            _active = true;
            _chosenIndex = -1;
            _question = _director.NextQuestion();
            for (int i = 0; i < _platforms.Length; i++) _platforms[i].ShowValue(_question.Choices[i]);

            _player.BeginAutopilot();
            _shownAt = Time.time;
            _shownTimeLimit = _timeLimit > 0f ? LeadDistance / Mathf.Max(player.RunSpeed, 0.01f) : 0f;
            // Subscribe before announcing the question: a listener may pick an answer the moment it appears.
            GameEvents.AnswerChosen += OnAnswerChosen;
            GameEvents.RaiseQuestionShown(_question, _shownTimeLimit);
        }

        private void OnAnswerChosen(int index)
        {
            if (!_active || _chosenIndex >= 0 || index < 0 || index >= _platforms.Length) return;

            _chosenIndex = index;
            _remainingFraction = _shownTimeLimit > 0f ? Mathf.Clamp01(1f - (Time.time - _shownAt) / _shownTimeLimit) : 0f;
            _platforms[index].Choose();
            float takeoffX = _platforms[index].transform.position.x - _landingAirTime * _player.RunSpeed;
            _player.QueueAutoJump(takeoffX);
            GameEvents.RaiseAnswerLocked(index);
        }

        private void Update()
        {
            if (!_active) return;
            float x = _player.transform.position.x;
            if (_chosenIndex < 0 && x >= DecisionX) Resolve(AnswerOutcome.Missed, -1);
            else if (x > EndX + 0.5f) Resolve(AnswerOutcome.Missed, -1);
        }

        internal void HandleLanding(int index)
        {
            if (!_active || index != _chosenIndex) return;
            Resolve(_question.IsCorrect(index) ? AnswerOutcome.Correct : AnswerOutcome.Wrong, index);
        }

        private void Resolve(AnswerOutcome outcome, int chosenIndex)
        {
            _active = false;
            _resolved = true;
            GameEvents.AnswerChosen -= OnAnswerChosen;
            _player.EndAutopilot();
            for (int i = 0; i < _platforms.Length; i++)
            {
                if (i == _question.CorrectIndex) _platforms[i].MarkCorrect();
                else if (i == chosenIndex) _platforms[i].Crumble();
                else _platforms[i].FadeOut();
            }
            float fraction = outcome == AnswerOutcome.Missed ? 0f : _remainingFraction;
            GameEvents.RaiseQuestionResolved(new QuestionResult(_question, outcome, chosenIndex, _shownTimeLimit, fraction));
        }
    }
}
