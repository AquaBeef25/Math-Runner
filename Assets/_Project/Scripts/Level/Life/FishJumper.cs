using UnityEngine;

namespace MathRunner.Level
{
    // Now and then a fish leaps out of a water pool and drops back in; hidden the rest of the time.
    public class FishJumper : LifeBody
    {
        [SerializeField] private float _period = 3.5f;
        [SerializeField] private float _width = 2f;
        [SerializeField] private float _height = 1.8f;
        private Vector3 _surface;

        public void Configure(Vector3 surface, float width, float phase)
        {
            _surface = surface;
            _width = width;
            Time01 = phase;
        }

        protected override void Update()
        {
            base.Update();
            var (offset, visible) = LifeMotion.FishLeap(Time01, _period, _width, _height);
            if (_renderer != null) _renderer.enabled = visible;
            transform.position = _surface + new Vector3(offset.x, offset.y, 0f);
            Animate(0, _frames.Length);
        }
    }
}
