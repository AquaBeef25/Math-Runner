using UnityEngine;

namespace MathRunner.Level
{
    // Loops a few sprite frames (e.g. the finish flag waving).
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 4f;

        private SpriteRenderer _renderer;

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        // Editor setup, tests and code-built pieces.
        public void SetFrames(Sprite[] frames, float framesPerSecond)
        {
            _frames = frames;
            _framesPerSecond = framesPerSecond;
        }

        private void Update()
        {
            if (_frames == null || _frames.Length == 0) return;
            _renderer.sprite = _frames[Mathf.FloorToInt(Time.time * _framesPerSecond) % _frames.Length];
        }
    }
}
