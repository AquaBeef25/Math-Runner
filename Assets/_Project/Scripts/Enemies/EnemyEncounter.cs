using MathRunner.Core;
using MathRunner.Math;
using MathRunner.Player;
using MathRunner.Systems;
using UnityEngine;

namespace MathRunner.Enemies
{
    // trigger → enemy flies in → wind-up → FIRE: Focus Zone (world slowed), question + 4 answers, bullet is the clock.
    // Right answer: shockwave destroys enemy and bullet. Wrong: the bullet hits (one heart). No answer: same when it lands.
    // The player is on autopilot (no jumping) from the trigger until the question is resolved.
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemyEncounter : MonoBehaviour
    {
        private enum Phase { Waiting, Arriving, Focus, Resolved }

        [SerializeField] private QuestionDirector _director;
        [SerializeField] private EncounterSettings _settings;
        [SerializeField] private FlyingEnemy _enemyPrefab;
        [SerializeField] private EnemyBullet _bulletPrefab;
        [SerializeField] private Shockwave _shockwavePrefab;

        [Header("This encounter")]
        [Tooltip("Real seconds from the shot to the bullet landing = the time to answer.")]
        [SerializeField, Min(0.1f)] private float _flightRealSeconds = 6f;
        [Tooltip("Whether a fast answer earns the speed bonus (the level's 'timed' questions).")]
        [SerializeField] private bool _speedBonus;

        private Phase _phase;
        private float _phaseTime;
        private bool _windingUp;
        private MathQuestion _question;
        private PlayerController _player;
        private PlayerHealth _health;
        private int _chosenIndex = -1;
        private float _remainingFraction;

        public bool IsFocusActive { get; private set; }
        public FlyingEnemy Enemy { get; private set; }
        public EnemyBullet Bullet { get; private set; }
        public float FlightRealSeconds => _flightRealSeconds;

        public void Configure(QuestionDirector director, float flightRealSeconds, bool speedBonus)
        {
            _director = director;
            _flightRealSeconds = Mathf.Max(flightRealSeconds, 0.1f);
            _speedBonus = speedBonus;
        }

        private void OnDisable()
        {
            GameEvents.AnswerChosen -= OnAnswerChosen;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_phase != Phase.Waiting || !other.TryGetComponent(out PlayerController player)) return;

            _player = player;
            _health = player.GetComponent<PlayerHealth>();
            _player.BeginAutopilot();
            Enemy = Instantiate(_enemyPrefab);
            Enemy.Appear(player.transform, _settings);
            _phase = Phase.Arriving;
            _phaseTime = 0f;
            _windingUp = false;
        }

        private void Update()
        {
            if (_phase != Phase.Arriving) return;
            _phaseTime += Time.deltaTime;
            if (!_windingUp && _phaseTime >= _settings.FlyInSeconds)
            {
                _windingUp = true;
                Enemy.WindUp();
            }
            if (_phaseTime >= _settings.FlyInSeconds + _settings.WindUpSeconds) Fire();
        }

        private void Fire()
        {
            _phase = Phase.Focus;
            _question = _director.NextQuestion();
            Bullet = Instantiate(_bulletPrefab, Enemy.MuzzlePosition, Quaternion.identity);
            Bullet.Arrived += OnBulletArrived;
            Bullet.Launch(_player.transform, _flightRealSeconds * _settings.FocusScale);

            IsFocusActive = true;
            GameEvents.RaiseFocusStarted(_settings.FocusScale);
            // Subscribe before announcing the question: a listener may pick an answer the moment it appears.
            GameEvents.AnswerChosen += OnAnswerChosen;
            GameEvents.RaiseQuestionShown(_question, _flightRealSeconds);
        }

        private void OnAnswerChosen(int index)
        {
            if (!IsFocusActive || _chosenIndex >= 0 || index < 0 || index >= _question.Choices.Count) return;

            _chosenIndex = index;
            _remainingFraction = _speedBonus ? Bullet.RemainingFraction : 0f;
            GameEvents.RaiseAnswerLocked(index);
            EndFocus();

            if (_question.IsCorrect(index))
            {
                Instantiate(_shockwavePrefab).Play(_player.transform.position, _settings.ShockwaveRadius, _settings.ShockwaveSeconds);
                Vector3 enemyAt = Enemy.transform.position;
                Enemy.Defeat();
                Bullet.Fizzle();
                GameEvents.RaiseEnemyDefeated(enemyAt);
                Resolve(AnswerOutcome.Correct);
            }
            else
            {
                Resolve(AnswerOutcome.Wrong); // the bullet keeps flying and costs the heart when it lands
            }
        }

        private void OnBulletArrived()
        {
            if (_health != null) _health.HitByBullet();
            else GameEvents.RaisePlayerHit(HitCause.Bullet);

            if (_chosenIndex < 0)
            {
                EndFocus();
                Resolve(AnswerOutcome.Missed);
            }
            if (Enemy != null) Enemy.Retreat();
        }

        private void EndFocus()
        {
            if (!IsFocusActive) return;
            IsFocusActive = false;
            GameEvents.RaiseFocusEnded();
        }

        private void Resolve(AnswerOutcome outcome)
        {
            _phase = Phase.Resolved;
            GameEvents.AnswerChosen -= OnAnswerChosen;
            _player.EndAutopilot();
            float fraction = outcome == AnswerOutcome.Missed ? 0f : _remainingFraction;
            GameEvents.RaiseQuestionResolved(new QuestionResult(_question, outcome, _chosenIndex, _flightRealSeconds, fraction));
        }
    }
}
