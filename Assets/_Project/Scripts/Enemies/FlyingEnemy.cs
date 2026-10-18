using MathRunner.UI;
using UnityEngine;

namespace MathRunner.Enemies
{
    // The jetpack enemy: flies in, hovers ahead of the player with a gentle bob, shakes before firing, then either
    // retreats (wrong answer) or pops (shockwave). Visual only; the encounter decides what happens. Scaled time throughout.
    public class FlyingEnemy : MonoBehaviour
    {
        private enum Mode { Idle, FlyIn, Hover, WindUp, Retreat }

        private static readonly Vector2 EntryOffset = new Vector2(10f, 6f);
        private static readonly Vector2 RetreatDirection = new Vector2(1f, 0.6f).normalized;
        private const float ShakeDistance = 0.08f;
        private const float ShakeHz = 30f;

        [SerializeField] private Transform _muzzle;
        [Tooltip("Optional burst left behind when the enemy is destroyed.")]
        [SerializeField] private ParticleSystem _puff;

        private Transform _player;
        private EncounterSettings _settings;
        private Mode _mode;
        private float _modeTime;
        private float _bobTime;
        private Vector3 _flyFrom;

        public Vector3 MuzzlePosition => _muzzle != null ? _muzzle.position : transform.position;

        // Pure so it can be tested: the hover point for a player position at a moment of the bob.
        public static Vector2 HoverPoint(Vector2 player, Vector2 offset, float time, float amplitude, float hz) =>
            player + offset + new Vector2(0f, amplitude * Mathf.Sin(2f * Mathf.PI * hz * time));

        public void Appear(Transform player, EncounterSettings settings)
        {
            _player = player;
            _settings = settings;
            _flyFrom = (Vector2)player.position + settings.HoverOffset + EntryOffset;
            transform.position = _flyFrom;
            Enter(Mode.FlyIn);
        }

        public void WindUp() => Enter(Mode.WindUp);

        public void Retreat() => Enter(Mode.Retreat);

        public void Defeat()
        {
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
            _bobTime += dt;
            Vector2 hover = HoverPoint(_player.position, _settings.HoverOffset, _bobTime, _settings.BobAmplitude, _settings.BobHz);

            switch (_mode)
            {
                case Mode.FlyIn:
                    float k = _settings.FlyInSeconds > 0f ? _modeTime / _settings.FlyInSeconds : 1f;
                    transform.position = Vector2.Lerp(_flyFrom, hover, Ease.OutQuad(k));
                    if (k >= 1f) Enter(Mode.Hover);
                    break;
                case Mode.Hover:
                    transform.position = hover;
                    break;
                case Mode.WindUp:
                    transform.position = hover + new Vector2(ShakeDistance * Mathf.Sin(2f * Mathf.PI * ShakeHz * _modeTime), 0f);
                    if (_modeTime >= _settings.WindUpSeconds) Enter(Mode.Hover);
                    break;
                case Mode.Retreat:
                    transform.position += (Vector3)(RetreatDirection * (_settings.RetreatSpeed * dt));
                    if (transform.position.x - _player.position.x > 30f) Destroy(gameObject);
                    break;
            }
        }
    }
}
