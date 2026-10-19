using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Math;
using MathRunner.Player;
using MathRunner.Systems;
using UnityEngine;

namespace MathRunner.Enemies
{
    // trigger → enemy arrives → wind-up → ATTACK: Focus Zone (world slowed), question + 4 answers, the attack is the clock.
    // The enemy is picked per encounter: the alien fires a fireball; a bee or fly is its own attack (charge,
    // buzz). Right answer: shockwave destroys enemy and attack. Wrong: the attack hits (one heart). No answer: same when
    // it lands.
    // The player keeps full control throughout: they navigate the terrain while answering.
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemyEncounter : MonoBehaviour
    {
        private enum Phase { Waiting, Arriving, Focus, Resolved }

        [SerializeField] private QuestionDirector _director;
        [SerializeField] private EncounterSettings _settings;
        [SerializeField] private FlyingEnemy _enemyPrefab;
        [SerializeField] private EnemyBullet _bulletPrefab;
        [SerializeField] private ChargingEnemy _beePrefab;
        [SerializeField] private ChargingEnemy _flyPrefab;
        [SerializeField] private Shockwave _shockwavePrefab;

        [Header("This encounter")]
        [Tooltip("Real seconds from the shot to the bullet landing = the time to answer.")]
        [SerializeField, Min(0.1f)] private float _flightRealSeconds = 6f;
        [Tooltip("Whether a fast answer earns the speed bonus (the level's 'timed' questions).")]
        [SerializeField] private bool _speedBonus;
        [Tooltip("Who this encounter meets (set by the level planner). Alien = the fireball enemy.")]
        [SerializeField] private EnemyKind _kind = EnemyKind.Alien;
        [Tooltip("Alien only: hover spot relative to the runner (zero = the usual spot).")]
        [SerializeField] private Vector2 _spot;

        private Phase _phase;
        private float _phaseTime;
        private bool _windingUp;
        private MathQuestion _question;
        private PlayerController _player;
        private PlayerHealth _health;
        private int _chosenIndex = -1;
        private float _remainingFraction;

        public bool IsFocusActive { get; private set; }
        // The alien and its fireball (alien encounters only).
        public FlyingEnemy Enemy { get; private set; }
        public EnemyBullet Bullet { get; private set; }
        // The bee or fly (its own attack).
        public ChargingEnemy Charger { get; private set; }
        public IEncounterAttack Attack { get; private set; }
        public EnemyKind Kind => _kind;
        public float FlightRealSeconds => _flightRealSeconds;

        public void Configure(QuestionDirector director, float flightRealSeconds, bool speedBonus,
            EnemyKind kind = EnemyKind.Alien, Vector2 spot = default)
        {
            _director = director;
            _flightRealSeconds = Mathf.Max(flightRealSeconds, 0.1f);
            _speedBonus = speedBonus;
            SetEnemy(kind, spot);
        }

        public void SetEnemy(EnemyKind kind, Vector2 spot)
        {
            _kind = kind;
            _spot = spot;
        }

        // Editor setup: the bee and fly prefabs.
        public void SetChargerPrefabs(ChargingEnemy bee, ChargingEnemy fly)
        {
            _beePrefab = bee;
            _flyPrefab = fly;
        }

        private ChargingEnemy ChargerPrefab() =>
            _kind == EnemyKind.Bee ? _beePrefab : _kind == EnemyKind.Fly ? _flyPrefab : null;

        private void OnDisable()
        {
            GameEvents.AnswerChosen -= OnAnswerChosen;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_phase != Phase.Waiting || !other.TryGetComponent(out PlayerController player)) return;

            _player = player;
            _health = player.GetComponent<PlayerHealth>();
            var chargerPrefab = ChargerPrefab();
            if (chargerPrefab != null)
            {
                Charger = Instantiate(chargerPrefab);
                Charger.Appear(player.transform, _settings);
            }
            else
            {
                Enemy = Instantiate(_enemyPrefab);
                Enemy.Appear(player.transform, _settings, _spot);
            }
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
                if (Charger != null) Charger.WindUp();
                else Enemy.WindUp();
            }
            if (_phaseTime >= _settings.FlyInSeconds + _settings.WindUpSeconds) Fire();
        }

        private void Fire()
        {
            _phase = Phase.Focus;
            _question = _director.NextQuestion();
            if (Charger != null) Attack = Charger;
            else Attack = Bullet = Instantiate(_bulletPrefab, Enemy.MuzzlePosition, Quaternion.identity);
            Attack.Arrived += OnAttackArrived;
            Attack.Launch(_player.transform, _flightRealSeconds * _settings.FocusScale);

            IsFocusActive = true;
            GameEvents.RaiseAnswerModeSet(AnswerMode.Tap);
            GameEvents.RaiseFocusStarted(_settings.FocusScale);
            // Subscribe before announcing the question: a listener may pick an answer the moment it appears.
            GameEvents.AnswerChosen += OnAnswerChosen;
            GameEvents.RaiseQuestionShown(_question, _flightRealSeconds);
        }

        private void OnAnswerChosen(int index)
        {
            if (!IsFocusActive || _chosenIndex >= 0 || index < 0 || index >= _question.Choices.Count) return;

            _chosenIndex = index;
            _remainingFraction = _speedBonus ? Attack.RemainingFraction : 0f;
            GameEvents.RaiseAnswerLocked(index);
            EndFocus();

            if (_question.IsCorrect(index))
            {
                Instantiate(_shockwavePrefab).Play(_player.transform.position, _settings.ShockwaveRadius, _settings.ShockwaveSeconds);
                if (FollowingFly.Current != null) FollowingFly.Current.Kill(); // the explosion also gets a following fly
                Vector3 enemyAt;
                if (Charger != null)
                {
                    enemyAt = Charger.transform.position;
                    Charger.Defeat();
                }
                else
                {
                    enemyAt = Enemy.transform.position;
                    Enemy.Defeat();
                    Bullet.Fizzle();
                }
                GameEvents.RaiseEnemyDefeated(enemyAt);
                Resolve(AnswerOutcome.Correct);
            }
            else
            {
                // The fireball or bee strikes right away (a fly instead starts following when its time is up); a fly
                // already following dives in with it.
                if (_kind != EnemyKind.Fly)
                {
                    Attack.Rush(_settings.WrongRushSeconds);
                    if (FollowingFly.Current != null) FollowingFly.Current.Lunge(_settings.WrongRushSeconds);
                }
                Resolve(AnswerOutcome.Wrong);
            }
        }

        private void OnAttackArrived()
        {
            // A fly that was answered wrong doesn't bite yet: it follows the runner to the next encounter.
            var followingAlready = FollowingFly.Current;
            if (_kind == EnemyKind.Fly && Charger != null)
            {
                var fly = Charger;
                Charger = null;
                fly.Release();
                fly.gameObject.AddComponent<FollowingFly>().Begin(_player.transform, fly.Renderer, fly.RestFrames);
            }
            else if (_health != null) _health.HitByBullet();
            else GameEvents.RaisePlayerHit(HitCause.Bullet);
            // A fly from an earlier wrong answer bites now too (it adds up), then flies away.
            if (followingAlready != null)
            {
                followingAlready.BiteAndLeave(_health);
                GameEvents.RaiseDoubleHit();
            }

            if (_chosenIndex < 0)
            {
                EndFocus();
                Resolve(AnswerOutcome.Missed);
            }
            if (Enemy != null) Enemy.Retreat();
            if (Charger != null) Charger.Retreat();
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
            float fraction = outcome == AnswerOutcome.Missed ? 0f : _remainingFraction;
            GameEvents.RaiseQuestionResolved(new QuestionResult(_question, outcome, _chosenIndex, _flightRealSeconds, fraction));
        }
    }
}
