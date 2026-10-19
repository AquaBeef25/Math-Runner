using MathRunner.Core;
using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Enemies
{
    // A fly that wasn't stopped: it follows the runner (harmless while following) until the next encounter decides its
    // fate. Next answer right: the explosion kills it. Next answer wrong: it lunges at the runner together with that
    // encounter's attack and bites as it lands (one more heart, shown as a double hit), then flies away. It also leaves
    // at the finish. Only one follows at a time; a scene reload forgets it. Scaled time throughout.
    public class FollowingFly : MonoBehaviour
    {
        private static readonly Vector2 FollowOffset = new Vector2(-1.4f, 1.8f);
        private static readonly Vector2 LeaveDirection = new Vector2(0.6f, 1f).normalized;
        private const float LeaveSpeed = 10f;

        public static FollowingFly Current { get; private set; }

        private Transform _player;
        private SpriteRenderer _renderer;
        private Sprite[] _frames = new Sprite[0];
        private float _time;
        private bool _leaving;
        private bool _lunging;
        private float _lungeSeconds;
        private float _lungeTime;
        private Vector2 _lungeFrom;

        public void Begin(Transform player, SpriteRenderer renderer, Sprite[] frames)
        {
            if (Current != null && Current != this) Current.Leave();
            Current = this;
            _player = player;
            _renderer = renderer;
            _frames = frames ?? new Sprite[0];
            GameEvents.LevelCompleted += Leave;
        }

        // The next encounter's shockwave got it.
        public void Kill()
        {
            Forget();
            Destroy(gameObject);
        }

        // The next encounter went wrong: dive at the runner over `seconds` (it bites when the attack lands).
        public void Lunge(float seconds)
        {
            _lunging = true;
            _lungeSeconds = Mathf.Max(seconds, 0.01f);
            _lungeTime = 0f;
            _lungeFrom = transform.position;
        }

        // The next encounter went wrong: one more heart, then it's gone.
        public void BiteAndLeave(PlayerHealth health)
        {
            if (health != null) health.HitByBullet();
            else GameEvents.RaisePlayerHit(HitCause.Bullet);
            Leave();
        }

        public void Leave()
        {
            Forget();
            _leaving = true;
        }

        private void Forget()
        {
            GameEvents.LevelCompleted -= Leave;
            if (Current == this) Current = null;
        }

        private void OnDestroy() => Forget();

        private void Update()
        {
            if (_player == null)
            {
                Destroy(gameObject);
                return;
            }
            float dt = Time.deltaTime;
            _time += dt;
            if (_lunging && !_leaving)
            {
                _lungeTime += dt;
                float k = Mathf.Clamp01(_lungeTime / _lungeSeconds);
                transform.position = Vector2.Lerp(_lungeFrom, (Vector2)_player.position + new Vector2(0.2f, 0.3f), k * k);
            }
            else if (_leaving)
            {
                transform.position += (Vector3)(LeaveDirection * (LeaveSpeed * dt));
                if (Vector2.Distance(transform.position, _player.position) > 20f) Destroy(gameObject);
            }
            else
            {
                Vector2 target = (Vector2)_player.position + FollowOffset + new Vector2(0.3f * Mathf.Sin(_time * 2.3f), 0.25f * Mathf.Sin(_time * 4.1f));
                transform.position = Vector2.Lerp(transform.position, target, 1f - Mathf.Exp(-6f * dt));
            }
            if (_renderer != null && _frames.Length > 0) _renderer.sprite = _frames[Mathf.FloorToInt(_time * 10f) % _frames.Length];
        }
    }
}
