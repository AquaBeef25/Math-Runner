using UnityEngine;

namespace MathRunner.Level
{
    // Shared by the life pieces: a looping frame animation on one SpriteRenderer, in world (scaled) time.
    public abstract class LifeBody : MonoBehaviour
    {
        [SerializeField] protected SpriteRenderer _renderer;
        [SerializeField] protected Sprite[] _frames = new Sprite[0];
        [SerializeField] protected float _framesPerSecond = 6f;

        protected float Time01;

        public void SetBody(SpriteRenderer renderer, Sprite[] frames, float framesPerSecond)
        {
            _renderer = renderer;
            _frames = frames ?? new Sprite[0];
            _framesPerSecond = framesPerSecond;
            if (_frames.Length > 0) _renderer.sprite = _frames[0];
        }

        protected void Animate(int first, int count)
        {
            if (_renderer == null || _frames.Length == 0) return;
            count = Mathf.Clamp(count, 1, _frames.Length - first);
            _renderer.sprite = _frames[first + Mathf.FloorToInt(Time01 * _framesPerSecond) % count];
        }

        protected virtual void Update() => Time01 += Time.deltaTime;
    }
}
