using UnityEngine;

namespace MathRunner.Level
{
    // A slime pacing back and forth between two points on flat ground. Touching it hurts (via Obstacle on the same object).
    public class Walker : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _visual;
        [SerializeField, Min(0f)] private float _speed = 1f;

        private float _left;
        private float _right;
        private float _time;
        private float _lastX;

        // Pure so it can be tested: position along the patrol at a moment (starts at the left bound, ping-pongs).
        public static float PatrolX(float left, float right, float speed, float time) =>
            left + Mathf.PingPong(time * speed, Mathf.Max(right - left, 0f));

        public void Configure(float left, float right, float speed)
        {
            _left = left;
            _right = right;
            _speed = speed;
            _time = 0f;
            transform.position = new Vector3(left, transform.position.y, transform.position.z);
            _lastX = left;
        }

        private void Awake()
        {
            if (_right <= _left) Configure(transform.position.x, transform.position.x + LevelPlanner.WalkerPatrol, _speed);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float x = PatrolX(_left, _right, _speed, _time);
            if (_visual != null && !Mathf.Approximately(x, _lastX)) _visual.flipX = x > _lastX; // art faces left
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
            _lastX = x;
        }
    }
}
