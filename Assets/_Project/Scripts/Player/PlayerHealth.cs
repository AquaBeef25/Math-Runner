using MathRunner.Core;
using UnityEngine;

namespace MathRunner.Player
{
    // Turns obstacle hits and falls into PlayerHit events, with a short invincible (flashing) period afterwards.
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private float _invincibleSeconds = 1.5f;
        [SerializeField] private float _flashInterval = 0.1f;

        private PlayerController _controller;
        private Vector2 _checkpoint;
        private float _invincibleUntil;

        public bool IsInvincible => Time.time < _invincibleUntil;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _checkpoint = transform.position;
        }

        public void SetCheckpoint(Vector2 position) => _checkpoint = position;

        public void HitByObstacle()
        {
            if (IsInvincible) return;
            _invincibleUntil = Time.time + _invincibleSeconds;
            GameEvents.RaisePlayerHit(HitCause.Obstacle);
        }

        // A wrong or missed answer always costs its heart, even right after another hit, and never moves the player.
        public void HitByBullet()
        {
            _invincibleUntil = Time.time + _invincibleSeconds;
            GameEvents.RaisePlayerHit(HitCause.Bullet);
        }

        // A wrong route at a fork: like a wrong answer, it always costs its heart and never moves the player.
        public void HitByWrongRoute()
        {
            _invincibleUntil = Time.time + _invincibleSeconds;
            GameEvents.RaisePlayerHit(HitCause.Mud);
        }

        public void Fell()
        {
            _controller.Respawn(_checkpoint);
            _invincibleUntil = Time.time + _invincibleSeconds;
            GameEvents.RaisePlayerHit(HitCause.Fall);
        }

        private void Update()
        {
            float remaining = _invincibleUntil - Time.time;
            _sprite.enabled = remaining <= 0f || (int)(remaining / _flashInterval) % 2 == 0;
        }
    }
}
