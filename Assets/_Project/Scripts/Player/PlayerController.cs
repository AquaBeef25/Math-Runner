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

        public float RunSpeed
        {
            get => _runSpeed;
            set => _runSpeed = value;
        }

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _baseGravityScale = _rb.gravityScale;
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

        private void OnJumpPressed()
        {
            _bufferCounter = _jumpBufferTime;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 v = _rb.linearVelocity;

            IsGrounded = CheckGrounded(v.y);
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
                IsGrounded = false;
            }

            if (_isJumping && v.y > 0f && !_input.JumpHeld)
            {
                v.y *= _jumpCutMultiplier;
                _isJumping = false;
            }

            _rb.gravityScale = v.y < 0f ? _baseGravityScale * _fallGravityMultiplier : _baseGravityScale;
            v.y = Mathf.Max(v.y, -_maxFallSpeed);
            v.x = _runSpeed;
            _rb.linearVelocity = v;
        }

        private bool CheckGrounded(float verticalSpeed)
        {
            if (verticalSpeed > 0.1f) return false;

            Bounds b = _col.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - 0.05f);
            Vector2 size = new Vector2(b.size.x * 0.9f, 0.1f);
            return Physics2D.OverlapBox(center, size, 0f, _groundLayer) != null;
        }
    }
}
