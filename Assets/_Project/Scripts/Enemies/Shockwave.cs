using MathRunner.UI;
using UnityEngine;

namespace MathRunner.Enemies
{
    // Expanding energy ring from the player after a correct answer: grows fast, thins and fades, then removes itself.
    [RequireComponent(typeof(LineRenderer))]
    public class Shockwave : MonoBehaviour
    {
        private const int Segments = 64;

        private LineRenderer _line;
        private float _baseWidth;
        private Gradient _baseColors;
        private float _radius;
        private float _seconds;
        private float _time;
        private bool _playing;

        // Pure so it can be tested: ring radius at a point of the burst (0..1).
        public static float RadiusAt(float t01, float radius) => Ease.OutQuad(Mathf.Clamp01(t01)) * radius;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = Segments;
            _line.loop = true;
            _line.useWorldSpace = false;
            _baseWidth = _line.widthMultiplier;
            _baseColors = _line.colorGradient;
        }

        public void Play(Vector3 centre, float radius, float seconds)
        {
            transform.position = centre;
            _radius = radius;
            _seconds = Mathf.Max(seconds, 0.01f);
            _time = 0f;
            _playing = true;
            Draw(0f);
        }

        private void Update()
        {
            if (!_playing) return;
            _time += Time.deltaTime;
            float t = Mathf.Clamp01(_time / _seconds);
            Draw(t);
            if (t >= 1f) Destroy(gameObject);
        }

        private void Draw(float t)
        {
            float r = RadiusAt(t, _radius);
            for (int i = 0; i < Segments; i++)
            {
                float a = i * 2f * Mathf.PI / Segments;
                _line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            }
            _line.widthMultiplier = _baseWidth * (1f - t);

            var keys = _baseColors.alphaKeys;
            for (int i = 0; i < keys.Length; i++) keys[i].alpha *= 1f - t;
            var g = new Gradient();
            g.SetKeys(_baseColors.colorKeys, keys);
            _line.colorGradient = g;
        }
    }
}
