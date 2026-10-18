using MathRunner.Core;
using System;
using UnityEngine;
using MathRunner.Input;

namespace MathRunner.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _input;
        [SerializeField] private LayerMask _groundLayer;

        [Header("Run")]
        [SerializeField] private float _runSpeed = 6f;

        [Header("Jump")]
        [SerializeField] private float _jumpHeight = 3.2f;
        [SerializeField, Range(0.1f, 1f)] private float _jumpCutMultiplier = 0.5f;
        [Tooltip("Releasing jump earlier than this still gives this much jump, so a quick tap clears answer platforms.")]
        [SerializeField] private float _minJumpTime = 0.08f;
        [SerializeField] private float _fallGravityMultiplier = 1.4f;
        [SerializeField] private float _maxFallSpeed = 25f;

        [Header("Forgiveness")]
        [SerializeField] private float _coyoteTime = 0.1f;
        [SerializeField] private float _jumpBufferTime = 0.1f;

        private Rigidbody2D _rb;
        private BoxCollider2D _col;
        private float _baseGravityScale;
        private float _coyoteCounter;
        private float _bufferCounter;
        private bool _isJumping;
        private float _jumpTimer;
        private float _lastAirSpeedY;
        private float _heldRealSeconds; // how long jump has been held, in the player's own (unslowed) seconds
        private readonly Collider2D[] _groundHits = new Collider2D[4];
        private ContactFilter2D _groundFilter;

        public float RunSpeed
        {
            get => _runSpeed;
            set => _runSpeed = value;
        }

        public bool IsGrounded { get; private set; }

        // The jump's shape, for level planning (what a full jump can reach).
        public float JumpHeight => _jumpHeight;
        public float FallGravityMultiplier => _fallGravityMultiplier;
        public float BaseGravityScale => _rb != null ? _baseGravityScale : GetComponent<Rigidbody2D>().gravityScale;

        // For feedback only (dust, squash, sounds); gameplay never listens to these.
        public event Action Jumped;
        public event Action<float> Landed; // downward speed at touchdown

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _baseGravityScale = _rb.gravityScale;
            _groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = _groundLayer, useTriggers = false };
        }

        private void OnEnable()
        {
            _input.JumpPressed += OnJumpPressed;
        }

        private void OnDisable()
        {
            _input.JumpPressed -= OnJumpPressed;
        }

        public void Respawn(Vector2 position)
        {
            _rb.position = position;
            _rb.linearVelocity = Vector2.zero;
            _coyoteCounter = 0f;
            _bufferCounter = 0f;
            _isJumping = false;
        }

        public float AirTimeToLandOn(float height)
        {
            float gravityScale = _rb != null ? _baseGravityScale : GetComponent<Rigidbody2D>().gravityScale;
            return JumpMath.AirTimeToLand(_jumpHeight, Physics2D.gravity.y, gravityScale, _fallGravityMultiplier, height);
        }

        private void OnJumpPressed()
        {
            _bufferCounter = _jumpBufferTime;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 v = _rb.linearVelocity;

            bool wasGrounded = IsGrounded;
            IsGrounded = CheckGrounded(v.y);
            if (IsGrounded && !wasGrounded) Landed?.Invoke(-_lastAirSpeedY);
            if (IsGrounded)
            {
                _coyoteCounter = _coyoteTime;
                _isJumping = false;
            }
            else
            {
                _coyoteCounter -= dt;
            }
            _bufferCounter -= dt;

            if (_bufferCounter > 0f && _coyoteCounter > 0f)
            {
                v.y = Mathf.Sqrt(2f * _jumpHeight * Mathf.Abs(Physics2D.gravity.y) * _baseGravityScale);
                _bufferCounter = 0f;
                _coyoteCounter = 0f;
                _isJumping = true;
                _jumpTimer = 0f;
                _heldRealSeconds = 0f;
                IsGrounded = false;
                Jumped?.Invoke();
            }

            if (_isJumping)
            {
                _jumpTimer += dt;
                if (_input.JumpHeld) _heldRealSeconds += dt / WorldTime.FocusFactor;
            }
            // Cut once the jump has run (in physics time) as long as the button was held (in real time), so the same tap
            // gives the same arc in the Focus Zone as at full speed.
            if (_isJumping && v.y > 0f && !_input.JumpHeld && _jumpTimer >= Mathf.Max(_minJumpTime, _heldRealSeconds))
            {
                v.y *= _jumpCutMultiplier;
                _isJumping = false;
            }

            _rb.gravityScale = v.y < 0f ? _baseGravityScale * _fallGravityMultiplier : _baseGravityScale;
            v.y = Mathf.Max(v.y, -_maxFallSpeed);
            v.x = _runSpeed;
            _rb.linearVelocity = v;
            _lastAirSpeedY = v.y;
        }

        private bool CheckGrounded(float verticalSpeed)
        {
            if (verticalSpeed > 0.1f) return false;

            Bounds b = _col.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - 0.05f);
            Vector2 size = new Vector2(b.size.x * 0.9f, 0.1f);
            int count = Physics2D.OverlapBox(center, size, 0f, _groundFilter, _groundHits);
            // Only a surface at the feet counts: rising through a one-way ledge or brushing a step face is not standing.
            for (int i = 0; i < count; i++)
                if (_groundHits[i].bounds.max.y <= b.min.y + 0.1f) return true;
            return false;
        }
    }
}
