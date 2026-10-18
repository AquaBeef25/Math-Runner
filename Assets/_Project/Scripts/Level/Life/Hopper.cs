using UnityEngine;

namespace MathRunner.Level
{
    // Sits, then hops in place now and then (frog). Frames: idle, jump.
    public class Hopper : LifeBody
    {
        [SerializeField] private float _period = 3f;
        [SerializeField] private float _height = 0.8f;
        private float _baseY;

        private void Awake() => _baseY = transform.position.y;

        public void Configure(float baseY, float phase)
        {
            _baseY = baseY;
            Time01 = phase;
        }

        protected override void Update()
        {
            base.Update();
            float h = LifeMotion.HopHeight(Time01, _period, _height);
            transform.position = new Vector3(transform.position.x, _baseY + h, transform.position.z);
            if (_renderer != null && _frames.Length > 1) _renderer.sprite = h > 0f ? _frames[1] : _frames[0];
        }
    }
}
