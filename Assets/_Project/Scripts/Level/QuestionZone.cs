using MathRunner.Core;
using MathRunner.Math;
using MathRunner.Player;
using MathRunner.Systems;
using UnityEngine;

namespace MathRunner.Level
{
    // The trigger shows a question; three answer platforms sit runSpeed × timeLimit ahead of it,
    // side by side above the ground, so the player picks one by timing the jump.
    [RequireComponent(typeof(BoxCollider2D))]
    public class QuestionZone : MonoBehaviour
    {
        [SerializeField] private QuestionDirector _director;
        [SerializeField] private AnswerPlatform[] _platforms = new AnswerPlatform[3];

        [Header("Timing")]
        [Tooltip("Seconds from the question appearing to reaching the first platform. 0 = untimed.")]
        [SerializeField, Min(0f)] private float _timeLimit;
        [Tooltip("Run-up time used to place the platforms when the question is untimed.")]
        [SerializeField, Min(1f)] private float _untimedLeadTime = 4f;
        [Tooltip("Run speed used to place the platforms. The level builder sets this from the level's run speed.")]
        [SerializeField, Min(0.1f)] private float _layoutRunSpeed = 6f;

        [Header("Layout")]
        [SerializeField] private float _platformWidth = 3f;
        [SerializeField] private float _platformGap = 1.5f;
        [Tooltip("Height of the platform surface above the ground. Must leave room for the player to run underneath.")]
        [SerializeField] private float _platformTop = 1.4f;
        [SerializeField] private float _platformThickness = 0.3f;

        private MathQuestion _question;
        private float _shownTimeLimit;
        private PlayerController _player;
        private bool _active;
        private bool _resolved;

        public float LeadDistance => (_timeLimit > 0f ? _timeLimit : _untimedLeadTime) * _layoutRunSpeed;

        private float EndX => transform.position.x + LeadDistance
            + _platformWidth * _platforms.Length + _platformGap * (_platforms.Length - 1);

        public void Configure(float runSpeed, float timeLimit)
        {
            _layoutRunSpeed = runSpeed;
            _timeLimit = timeLimit;
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

        private void ApplyLayout()
        {
            for (int i = 0; i < _platforms.Length; i++)
            {
                if (_platforms[i] == null) continue;
                float x = LeadDistance + _platformWidth * 0.5f + i * (_platformWidth + _platformGap);
                var center = new Vector2(x, _platformTop - _platformThickness * 0.5f);
                _platforms[i].SetLayout(center, new Vector2(_platformWidth, _platformThickness));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_active || _resolved || !other.TryGetComponent(out PlayerController player)) return;

            _player = player;
            _active = true;
            _question = _director.NextQuestion();
            for (int i = 0; i < _platforms.Length; i++) _platforms[i].ShowValue(_question.Choices[i]);

            _shownTimeLimit = _timeLimit > 0f ? LeadDistance / Mathf.Max(player.RunSpeed, 0.01f) : 0f;
            GameEvents.RaiseQuestionShown(_question, _shownTimeLimit);
        }

        private void Update()
        {
            if (_active && _player.transform.position.x > EndX + 0.5f) Resolve(AnswerOutcome.Missed, -1);
        }

        internal void HandleLanding(int index)
        {
            if (!_active) return;
            Resolve(_question.IsCorrect(index) ? AnswerOutcome.Correct : AnswerOutcome.Wrong, index);
        }

        private void Resolve(AnswerOutcome outcome, int chosenIndex)
        {
            _active = false;
            _resolved = true;
            for (int i = 0; i < _platforms.Length; i++)
            {
                if (i == _question.CorrectIndex) _platforms[i].MarkCorrect();
                else if (i == chosenIndex) _platforms[i].Crumble();
                else _platforms[i].FadeOut();
            }
            GameEvents.RaiseQuestionResolved(new QuestionResult(_question, outcome, chosenIndex, _shownTimeLimit));
        }
    }
}
