using UnityEngine;

namespace MathRunner.Level
{
    // A small emote bubble that pops over its owner once, for about a second, when the runner passes by.
    public class EmoteBubble : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _bubble;
        [SerializeField] private float _range = 3f;
        [SerializeField] private float _seconds = 1f;
        private Transform _player;
        private float _shownFor = -1f;

        public void Configure(Transform player, SpriteRenderer bubble)
        {
            _player = player;
            _bubble = bubble;
            _bubble.enabled = false;
        }

        private void Update()
        {
            if (_player == null || _bubble == null) return;
            if (_shownFor < 0f)
            {
                if (Mathf.Abs(_player.position.x - transform.position.x) < _range)
                {
                    _shownFor = 0f;
                    _bubble.enabled = true;
                }
                return;
            }
            if (!_bubble.enabled) return;
            _shownFor += Time.deltaTime;
            float pop = Mathf.Clamp01(_shownFor / 0.15f);
            _bubble.transform.localScale = Vector3.one * pop;
            if (_shownFor >= _seconds) _bubble.enabled = false;
        }
    }
}
