using UnityEngine;

namespace MathRunner.Level
{
    // A critter pacing a short stretch of flat ground and back (snail, mouse, worm, ladybug). Faces where it walks.
    public class Critter : LifeBody
    {
        [SerializeField] private float _left;
        [SerializeField] private float _right;
        [SerializeField] private float _speed = 0.6f;
        private float _lastX;

        public void Configure(float left, float right, float speed)
        {
            _left = left;
            _right = right;
            _speed = speed;
            _lastX = left;
        }

        protected override void Update()
        {
            base.Update();
            float x = Walker.PatrolX(_left, _right, _speed, Time01);
            if (_renderer != null && !Mathf.Approximately(x, _lastX)) _renderer.flipX = x > _lastX; // Kenney art faces left
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
            _lastX = x;
            Animate(0, _frames.Length);
        }
    }
}
