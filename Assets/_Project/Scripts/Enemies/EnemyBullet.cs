using System;
using UnityEngine;

namespace MathRunner.Enemies
{
    // A shot that always arrives exactly when its flight time runs out: each frame it covers its share of the remaining
    // distance to the (moving) target. Runs on scaled time, so it crawls during the Focus Zone.
    public class EnemyBullet : MonoBehaviour
    {
        [SerializeField] private EncounterSettings _settings;

        private Transform _target;
        private float _total;
        private float _remaining;
        private bool _flying;

        // Fires once when the bullet reaches its target; the bullet removes itself right after.
        public event Action Arrived;

        public float RemainingFraction => _total > 0f ? Mathf.Clamp01(_remaining / _total) : 0f;

        // Pure so it can be tested: where the bullet is after dt seconds with `remaining` seconds of flight left.
        public static Vector2 Step(Vector2 position, Vector2 target, float remainingSeconds, float deltaSeconds)
        {
            if (deltaSeconds >= remainingSeconds) return target;
            return Vector2.Lerp(position, target, deltaSeconds / remainingSeconds);
        }

        public void Launch(Transform target, float flightSeconds)
        {
            _target = target;
            _total = Mathf.Max(flightSeconds, 0.01f);
            _remaining = _total;
            _flying = true;
        }

        // Removed by the shockwave: no hit.
        public void Fizzle()
        {
            _flying = false;
            Arrived = null;
            Destroy(gameObject);
        }

        private void Update()
        {
            if (!_flying) return;
            if (_target == null)
            {
                Fizzle();
                return;
            }

            float dt = Time.deltaTime;
            transform.position = Step(transform.position, _target.position, _remaining, dt);
            if (_settings != null) transform.Rotate(0f, 0f, _settings.BulletSpinDegreesPerSecond * dt);
            _remaining -= dt;
            if (_remaining > 0f) return;

            _flying = false;
            Arrived?.Invoke();
            Destroy(gameObject);
        }
    }
}
