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
