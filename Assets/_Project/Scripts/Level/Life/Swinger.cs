using UnityEngine;

namespace MathRunner.Level
{
    // A crate hanging on a chain from above the screen, swinging gently. Rotates its own (pivot) transform.
    public class Swinger : MonoBehaviour
    {
        [SerializeField] private float _maxDegrees = 12f;
        [SerializeField] private float _hz = 0.3f;
        private float _time;

        public void Configure(float phase) => _time = phase;

        private void Update()
        {
            _time += Time.deltaTime;
            transform.localRotation = Quaternion.Euler(0f, 0f, LifeMotion.SwingAngle(_time, _maxDegrees, _hz));
        }
    }
}
