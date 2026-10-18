using UnityEngine;

namespace MathRunner.Level
{
    // Drifts in a lazy figure-eight around a spot, wings flapping (fly, ladybug).
    public class Flyer : LifeBody
    {
        [SerializeField] private float _radius = 0.8f;
        [SerializeField] private float _hz = 0.25f;
        private Vector3 _center;
        private float _lastX;

        private void Awake() => _center = transform.position;

        public void Configure(Vector3 center, float phase)
        {
            _center = center;
            Time01 = phase;
        }

        protected override void Update()
        {
            base.Update();
            var o = LifeMotion.FlyerOffset(Time01, _radius, _hz);
            transform.position = _center + new Vector3(o.x, o.y, 0f);
            if (_renderer != null && !Mathf.Approximately(o.x, _lastX)) _renderer.flipX = o.x > _lastX;
            _lastX = o.x;
            Animate(0, _frames.Length);
        }
    }
}
