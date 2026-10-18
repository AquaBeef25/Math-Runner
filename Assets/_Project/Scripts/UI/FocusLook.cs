using MathRunner.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace MathRunner.UI
{
    // Focus Zone look only: fades in a darker, less colourful screen (a URP Volume) and a glow around the player while
    // an enemy's shot is in the air. Fades on unscaled time so it doesn't crawl along with the slowed world.
    public class FocusLook : MonoBehaviour
    {
        [SerializeField] private Volume _volume;
        [Tooltip("Optional soft glow sprite on the player.")]
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField, Min(0.01f)] private float _fadeSeconds = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.45f;

        private float _weight;
        private float _target;

        private void Awake() => Apply(0f);

        private void OnEnable()
        {
            GameEvents.FocusStarted += OnFocusStarted;
            GameEvents.FocusEnded += OnFocusEnded;
        }

        private void OnDisable()
        {
            GameEvents.FocusStarted -= OnFocusStarted;
            GameEvents.FocusEnded -= OnFocusEnded;
        }

        private void OnFocusStarted(float factor) => _target = 1f;

        private void OnFocusEnded() => _target = 0f;

        private void Update()
        {
            if (Mathf.Approximately(_weight, _target)) return;
            Apply(Mathf.MoveTowards(_weight, _target, Time.unscaledDeltaTime / _fadeSeconds));
        }

        private void Apply(float weight)
        {
            _weight = weight;
            if (_volume != null) _volume.weight = weight;
            if (_glow == null) return;
            _glow.enabled = weight > 0.001f;
            var c = _glow.color;
            c.a = weight * _glowAlpha;
            _glow.color = c;
        }
    }
}
