using UnityEngine;

namespace MathRunner.Level
{
    // A spinning saw blade on a surface. Visual spin only; the Obstacle on the same object does the damage.
    public class Saw : MonoBehaviour
    {
        [SerializeField] private Transform _blade;
        [SerializeField] private float _degreesPerSecond = -360f;

        private void Update()
        {
            if (_blade != null) _blade.Rotate(0f, 0f, _degreesPerSecond * Time.deltaTime);
        }
    }
}
