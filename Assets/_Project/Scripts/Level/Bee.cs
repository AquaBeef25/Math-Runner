using UnityEngine;

namespace MathRunner.Level
{
    // A bee hovering in place, bobbing up and down at about head height: jump over it. Its trigger collider and the
    // Obstacle on the same object do the damage; a SpriteFlipbook beats the wings.
    public class Bee : MonoBehaviour
    {
        [SerializeField] private float _amplitude = 0.4f;
        [SerializeField] private float _hz = 0.8f;

        private float _baseY;
        private float _phase;

        public void Configure(float x, float baseY, float amplitude, float hz)
        {
            _baseY = baseY;
            _amplitude = amplitude;
            _hz = hz;
            _phase = x * 0.37f; // neighbouring bees don't bob in step
            transform.position = new Vector3(x, BobY(baseY, amplitude, hz, _phase), 0f);
        }

        private void Awake() => _baseY = transform.position.y;

        private void Update()
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, BobY(_baseY, _amplitude, _hz, Time.time + _phase), p.z);
        }

        public static float BobY(float baseY, float amplitude, float hz, float time) =>
            baseY + amplitude * Mathf.Sin(2f * Mathf.PI * hz * time);
    }
}
