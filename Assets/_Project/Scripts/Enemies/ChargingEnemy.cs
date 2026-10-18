using System;
using MathRunner.UI;
using UnityEngine;

namespace MathRunner.Enemies
{
    // An encounter enemy that is its own attack (bee, frog, fly): flies or hops in, waits ahead of the runner, shakes,
    // then charges / leaps / buzzes at the runner and arrives exactly when the question's time runs out. Pops on the
    // shockwave. Visual only (no solid collider); the encounter decides what the arrival costs. Scaled time throughout.
    public class ChargingEnemy : MonoBehaviour, IEncounterAttack
    {
        public enum Style { Charge, Leap, Buzz }

        private enum Mode { Idle, Approach, Wait, WindUp, Attack, Retreat }

        private static readonly Vector2 RetreatDirection = new Vector2(1f, 0.8f).normalized;
        private const float ShakeDistance = 0.08f;
        private const float ShakeHz = 30f;

        [SerializeField] private Style _style;
        [Tooltip("Where it waits, relative to the runner (the frog sits on the ground: y 0 = the runner's feet).")]
        [SerializeField] private Vector2 _waitOffset = new Vector2(6f, 2.5f);
        [Tooltip("Where it comes from, relative to its waiting spot.")]
        [SerializeField] private Vector2 _entryOffset = new Vector2(8f, 4f);
        [SerializeField, Min(0f)] private float _leapPeak = 2.5f;
        [SerializeField, Min(0f)] private float _buzzAmplitude = 0.6f;
        [SerializeField, Min(0f)] private float _buzzHz = 3f;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _restFrames = new Sprite[0];
        [SerializeField] private Sprite[] _attackFrames = new Sprite[0];
        [SerializeField] private float _framesPerSecond = 8f;
        [SerializeField] private ParticleSystem _puff;

        private Transform _player;
        private EncounterSettings _settings;
        private Mode _mode;
        private float _modeTime;
        private float _time;
        private Vector3 _from;
        private Vector2 _base;
        private Transform _target;
        private float _total;
        private float _remaining;
        private float _smoothY;
        private float _smoothYVelocity;

        public event Action Arrived;

        public Style AttackStyle => _style;
        public float RemainingFraction => _total > 0f ? Mathf.Clamp01(_remaining / _total) : 0f;
        public bool IsAttacking => _mode == Mode.Attack;

        // Editor setup and tests.
        public void Setup(Style style, Vector2 waitOffset, SpriteRenderer renderer, Sprite[] restFrames, Sprite[] attackFrames)
        {
            _style = style;
            _waitOffset = waitOffset;
            _renderer = renderer;
            _restFrames = restFrames ?? new Sprite[0];
            _attackFrames = attackFrames ?? new Sprite[0];
        }

        public void Appear(Transform player, EncounterSettings settings)
        {
            _player = player;
            _settings = settings;
            _smoothY = FeetY();
            _from = (Vector3)(WaitPoint() + _entryOffset);
            transform.position = _from;
            Enter(Mode.Approach);
        }

        public void WindUp() => Enter(Mode.WindUp);

        public void Launch(Transform target, float flightSeconds)
        {
            _target = target;
            _total = Mathf.Max(flightSeconds, 0.01f);
            _remaining = _total;
            _base = transform.position;
            Enter(Mode.Attack);
        }

        public void Retreat()
        {
            Arrived = null;
            Enter(Mode.Retreat);
        }

        // Shockwave: no hit, the enemy pops.
        public void Fizzle() => Defeat();

        public void Defeat()
        {
            Arrived = null;
            if (_puff != null)
            {
                _puff.transform.SetParent(null, true);
                _puff.Play();
                Destroy(_puff.gameObject, 2f);
            }
            Destroy(gameObject);
        }

        private void Enter(Mode mode)
        {
            _mode = mode;
            _modeTime = 0f;
        }

        private float FeetY() => _player.position.y - 0.5f;

        private Vector2 WaitPoint() => new Vector2(_player.position.x, _smoothY) + _waitOffset;

        private void Update()
        {
            if (_mode == Mode.Idle) return;
            if (_player == null)
            {
                Destroy(gameObject);
                return;
            }

            float dt = Time.deltaTime;
            _modeTime += dt;
            _time += dt;
            _smoothY = Mathf.SmoothDamp(_smoothY, FeetY(), ref _smoothYVelocity, 0.25f, Mathf.Infinity, dt);
            Vector2 wait = WaitPoint();
            if (_style != Style.Leap) wait += new Vector2(0f, 0.2f * Mathf.Sin(2f * Mathf.PI * 1.5f * _time)); // hover bob

            switch (_mode)
            {
                case Mode.Approach:
                    float k = _settings != null && _settings.FlyInSeconds > 0f ? _modeTime / _settings.FlyInSeconds : 1f;
                    Vector2 p = Vector2.Lerp(_from, wait, Ease.OutQuad(k));
                    if (_style == Style.Leap) p.y = Mathf.Lerp(_from.y, wait.y, k) + AttackPath.LeapHeight(k, 1.5f) * (1f - k);
                    transform.position = p;
                    if (k >= 1f) Enter(Mode.Wait);
                    break;
                case Mode.Wait:
                    transform.position = wait;
                    break;
                case Mode.WindUp:
                    transform.position = wait + new Vector2(ShakeDistance * Mathf.Sin(2f * Mathf.PI * ShakeHz * _modeTime), 0f);
                    break;
                case Mode.Attack:
                    StepAttack(dt);
                    break;
                case Mode.Retreat:
                    transform.position += (Vector3)(RetreatDirection * ((_settings != null ? _settings.RetreatSpeed : 12f) * dt));
                    if (Vector2.Distance(transform.position, _player.position) > 20f) Destroy(gameObject);
                    break;
            }
            Animate();
        }

        private void StepAttack(float dt)
        {
            if (_target == null)
            {
                Defeat();
                return;
            }
            _base = AttackPath.Charge(_base, _target.position, _remaining, dt);
            _remaining -= dt;
            float fraction = RemainingFraction;
            Vector2 extra = _style == Style.Leap ? new Vector2(0f, AttackPath.LeapHeight(1f - fraction, _leapPeak))
                : _style == Style.Buzz ? AttackPath.Buzz(_time, _buzzAmplitude, _buzzHz, fraction)
                : Vector2.zero;
            transform.position = _base + extra;
            if (_renderer != null) _renderer.flipX = false; // Kenney art faces left: toward the runner
            if (_remaining > 0f) return;

            Enter(Mode.Wait);
            var arrived = Arrived;
            Arrived = null;
            arrived?.Invoke();
        }

        private void Animate()
        {
            if (_renderer == null) return;
            var frames = _mode == Mode.Attack && _attackFrames.Length > 0 ? _attackFrames : _restFrames;
            if (frames.Length == 0) return;
            _renderer.sprite = frames[Mathf.FloorToInt(_time * _framesPerSecond) % frames.Length];
        }
    }
}
