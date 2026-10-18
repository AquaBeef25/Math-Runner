using MathRunner.Core;
using UnityEngine;

namespace MathRunner.Player
{
    public enum PlayerPose
    {
        Idle,
        Run,
        Jump,
        Fall,
        Hurt
    }

    // Visual only: picks the character sprite from PlayerController state and adds squash & stretch.
    // Lives on the Player; drives the SpriteRenderer on the "Visual" child so the collider never changes.
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _sprite;

        [Header("Sprites")]
        [SerializeField] private Sprite _idle;
        [SerializeField] private Sprite[] _run;
        [SerializeField] private Sprite _jump;
        [SerializeField] private Sprite _fall;
        [SerializeField] private Sprite _hurt;

        [Header("Timing")]
        [Tooltip("Run frames advance with distance, so the legs keep pace at any run speed.")]
        [SerializeField] private float _runFramesPerUnit = 2.5f;
        [SerializeField] private float _hurtSeconds = 0.4f;

        [Header("Squash & stretch")]
        [SerializeField] private Vector2 _jumpStretch = new Vector2(0.8f, 1.2f);
        [SerializeField] private Vector2 _landSquash = new Vector2(1.25f, 0.75f);
        [Tooltip("Landing speed (units/s) that gives the full squash; softer landings squash less.")]
        [SerializeField] private float _fullSquashSpeed = 15f;
        [SerializeField] private float _recoverSpeed = 12f;

        private PlayerController _controller;
        private Rigidbody2D _rb;
        private Vector2 _scale = Vector2.one;
        private float _hurtUntil = -1f;

        public PlayerPose Pose { get; private set; }

        // Pure so it can be tested: what to show for the current movement state.
        public static PlayerPose PickPose(bool grounded, Vector2 velocity, bool hurt)
        {
            if (hurt) return PlayerPose.Hurt;
            if (!grounded) return velocity.y > 0f ? PlayerPose.Jump : PlayerPose.Fall;
            return Mathf.Abs(velocity.x) > 0.1f ? PlayerPose.Run : PlayerPose.Idle;
        }

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _controller.Jumped += OnJumped;
            _controller.Landed += OnLanded;
            GameEvents.PlayerHit += OnPlayerHit;
        }

        private void OnDisable()
        {
            _controller.Jumped -= OnJumped;
            _controller.Landed -= OnLanded;
            GameEvents.PlayerHit -= OnPlayerHit;
        }

        private void OnJumped() => _scale = _jumpStretch;

        private void OnLanded(float downwardSpeed)
        {
            float k = Mathf.Clamp01(downwardSpeed / _fullSquashSpeed);
            _scale = Vector2.LerpUnclamped(Vector2.one, _landSquash, k);
        }

        private Sprite RunFrame()
        {
            int n = Mathf.FloorToInt(transform.position.x * _runFramesPerUnit) % _run.Length;
            return _run[n < 0 ? n + _run.Length : n]; // the start of a level can be left of x = 0
        }

        private void OnPlayerHit(HitCause cause) => _hurtUntil = Time.time + _hurtSeconds;

        private void LateUpdate()
        {
            Pose = PickPose(_controller.IsGrounded, _rb.linearVelocity, Time.time < _hurtUntil);
            _sprite.sprite = Pose switch
            {
                PlayerPose.Hurt => _hurt,
                PlayerPose.Jump => _jump,
                PlayerPose.Fall => _fall,
                PlayerPose.Run => RunFrame(),
                _ => _idle
            };

            _scale = Vector2.Lerp(_scale, Vector2.one, 1f - Mathf.Exp(-_recoverSpeed * Time.deltaTime));
            _sprite.transform.localScale = new Vector3(_scale.x, _scale.y, 1f);
        }
    }
}
