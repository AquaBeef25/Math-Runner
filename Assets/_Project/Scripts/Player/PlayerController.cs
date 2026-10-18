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
        private readonly RaycastHit2D[] _rayHits = new RaycastHit2D[4];
        private const float WalkableNormalY = 0.82f; // slopes up to ~35°
        private bool _jumpedSinceGrounded;
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
            // Over a crest or onto a down-slope the ground drops away a little each step: stick to it instead of flying off.
            if (!IsGrounded && wasGrounded && !_isJumping && v.y <= 0.1f && StickToGround()) IsGrounded = true;
            if (IsGrounded && !wasGrounded) Landed?.Invoke(-_lastAirSpeedY);
            if (IsGrounded)
            {
                _coyoteCounter = _coyoteTime;
                _isJumping = false;
                _jumpedSinceGrounded = false;
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
                _jumpedSinceGrounded = true;
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

            // On the ground and not jumping: move along the surface at full run speed (flat or ramp), with no gravity bob.
            bool onSurface = IsGrounded && !_isJumping;
            if (onSurface && SampleGroundSlope(out float slope)) v.y = _runSpeed * slope;
            _rb.gravityScale = onSurface ? 0f : v.y < 0f ? _baseGravityScale * _fallGravityMultiplier : _baseGravityScale;
            v.y = Mathf.Max(v.y, -_maxFallSpeed);
            v.x = _runSpeed;
            _rb.linearVelocity = v;
            _lastAirSpeedY = v.y;
        }

        private bool CheckGrounded(float verticalSpeed)
        {
            // Moving up because of a jump is never standing; moving up a ramp is.
            if (verticalSpeed > 0.1f && _jumpedSinceGrounded) return false;

            Bounds b = _col.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - 0.05f);
            Vector2 size = new Vector2(b.size.x * 0.9f, 0.1f);
            int count = Physics2D.OverlapBox(center, size, 0f, _groundFilter, _groundHits);
            for (int i = 0; i < count; i++)
            {
                // Rising through a one-way ledge isn't standing on it: only its top surface counts.
                if (_groundHits[i].usedByEffector && _groundHits[i].bounds.max.y > b.min.y + 0.1f) continue;
                return true;
            }
            return false;
        }

        // Slope (rise per unit of run) of the ground just ahead, from a ray under the front foot (the centre if nothing
        // is there). False when no walkable surface is close below.
        private const float ClimbAllowance = 0.05f;

        private bool SampleGroundSlope(out float slope)
        {
            Bounds b = _col.bounds;
            slope = float.NegativeInfinity;
            // Front foot, plus one physics step ahead of it; take the steeper reading so the climb starts before the box
            // reaches a ramp, but levelling off or heading down never starts early (that would bump or dip).
            float ahead = b.max.x + _runSpeed * Time.fixedDeltaTime;
            float highest = float.NegativeInfinity, aheadSlope = float.NegativeInfinity, frontSlope = float.NegativeInfinity;
            if (TryGroundHit(new Vector2(ahead, b.min.y + 0.3f), 0.8f, b.min.y, out var hit))
            {
                aheadSlope = -hit.normal.x / hit.normal.y;
                highest = Mathf.Max(highest, hit.point.y);
            }
            if (TryGroundHit(new Vector2(b.max.x - 0.05f, b.min.y + 0.3f), 0.8f, b.min.y, out hit))
            {
                frontSlope = -hit.normal.x / hit.normal.y;
                highest = Mathf.Max(highest, hit.point.y);
            }
            slope = Mathf.Max(aheadSlope, frontSlope);
            // Never climb past the highest ground in front of the box this step, or the steeper reading would launch it at
            // a crest or a ramp foot. Starting a climb gets a little extra so the box clears the ramp's edge.
            float step = _runSpeed * Time.fixedDeltaTime;
            float allowance = aheadSlope < frontSlope ? 0f : ClimbAllowance;
            if (slope > 0f && step > 0f) slope = Mathf.Min(slope, Mathf.Max(0f, highest - b.min.y + allowance) / step);
            if (float.IsNegativeInfinity(slope) && TryGroundHit(new Vector2(b.center.x, b.min.y + 0.3f), 0.8f, b.min.y, out hit))
                slope = -hit.normal.x / hit.normal.y;
            if (float.IsNegativeInfinity(slope))
            {
                slope = 0f;
                return false;
            }
            return true;
        }

        // Pull the feet down onto ground that is less than StickDistance below (crests, the top of a down-slope).
        private bool StickToGround()
        {
            const float StickDistance = 0.4f;
            Bounds b = _col.bounds;
            if (!TryGroundHit(new Vector2(b.center.x, b.min.y + 0.05f), StickDistance + 0.05f, b.min.y, out var hit) &&
                !TryGroundHit(new Vector2(b.max.x - 0.05f, b.min.y + 0.05f), StickDistance + 0.05f, b.min.y, out hit)) return false;
            float drop = b.min.y - hit.point.y;
            if (drop < 0f || drop > StickDistance) return false;
            _rb.position -= new Vector2(0f, drop);
            return true;
        }

        private bool TryGroundHit(Vector2 origin, float distance, float feetY, out RaycastHit2D hit)
        {
            int count = Physics2D.Raycast(origin, Vector2.down, _groundFilter, _rayHits, distance);
            for (int i = 0; i < count; i++)
            {
                hit = _rayHits[i];
                if (hit.normal.y < WalkableNormalY) continue;                                   // too steep / a wall
                if (hit.collider.usedByEffector && hit.collider.bounds.max.y > feetY + 0.1f) continue; // inside a ledge
                return true;
            }
            hit = default;
            return false;
        }
    }
}
