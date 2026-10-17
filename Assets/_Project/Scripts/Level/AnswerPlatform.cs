using MathRunner.Player;
using TMPro;
using UnityEngine;

namespace MathRunner.Level
{
    // Platform showing an answer. It is not solid until the player picks it; landing on it then resolves the question.
    [RequireComponent(typeof(BoxCollider2D))]
    public class AnswerPlatform : MonoBehaviour
    {
        private enum State { Idle, Armed, Correct, Crumbling, Faded }

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private TextMeshPro _label;
        [SerializeField] private float _labelHeight = 0.8f;

        [Header("Feedback")]
        [SerializeField] private Color _idleColor = new Color(1f, 0.78f, 0.25f);
        [SerializeField] private Color _chosenColor = new Color(1f, 0.95f, 0.6f);
        [SerializeField] private Color _correctColor = new Color(0.3f, 0.85f, 0.4f);
        [SerializeField] private Color _wrongColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private float _crumbleDelay = 0.15f;
        [SerializeField] private float _crumbleDuration = 0.5f;
        [SerializeField] private float _fadedAlpha = 0.3f;

        private BoxCollider2D _col;
        private QuestionZone _zone;
        private int _index;
        private State _state;
        private float _stateTime;
        private bool _chosen;

        private void Awake()
        {
            _col = GetComponent<BoxCollider2D>();
            _col.enabled = false;
            _body.color = _idleColor;
            _label.text = "?";
        }

        public void Bind(QuestionZone zone, int index)
        {
            _zone = zone;
            _index = index;
        }

        public void SetLayout(Vector2 localCenter, Vector2 size)
        {
            transform.localPosition = localCenter;
            GetComponent<BoxCollider2D>().size = size;
            if (_body != null) _body.transform.localScale = new Vector3(size.x, size.y, 1f);
            if (_label != null) _label.transform.localPosition = new Vector3(0f, size.y * 0.5f + _labelHeight, 0f);
        }

        public void ShowValue(int value)
        {
            _label.text = value.ToString();
            _state = State.Armed;
        }

        public void Choose()
        {
            _chosen = true;
            _col.enabled = true;
            _body.color = _chosenColor;
            _label.transform.localScale = Vector3.one * 1.2f;
        }

        public void MarkCorrect() => Enter(State.Correct, _correctColor);

        public void Crumble() => Enter(State.Crumbling, _wrongColor);

        public void FadeOut()
        {
            Enter(State.Faded, _body.color);
            _col.enabled = false;
            SetAlpha(_fadedAlpha);
        }

        private void Enter(State state, Color color)
        {
            _state = state;
            _stateTime = 0f;
            _body.color = color;
        }

        private void Update()
        {
            if (_state != State.Correct && _state != State.Crumbling) return;
            _stateTime += Time.deltaTime;

            if (_state == State.Correct)
            {
                float t = Mathf.Clamp01(_stateTime / 0.4f);
                _label.transform.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(t * Mathf.PI));
                return;
            }

            if (_stateTime < _crumbleDelay)
            {
                _body.transform.localPosition = new Vector3(Mathf.Sin(_stateTime * 80f) * 0.06f, 0f, 0f);
                return;
            }

            if (_col.enabled) _col.enabled = false;
            float fall = Mathf.Clamp01((_stateTime - _crumbleDelay) / _crumbleDuration);
            _body.transform.localPosition = new Vector3(0f, -1.5f * fall * fall, 0f);
            SetAlpha(1f - fall);
            if (fall >= 1f) _state = State.Faded;
        }

        private void SetAlpha(float alpha)
        {
            var c = _body.color;
            c.a = alpha;
            _body.color = c;
            _label.alpha = alpha;
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryLand(collision);

        private void OnCollisionStay2D(Collision2D collision) => TryLand(collision);

        private void TryLand(Collision2D collision)
        {
            if (_state != State.Armed || !_chosen) return;
            if (!collision.collider.TryGetComponent(out PlayerController _)) return;
            // Jumping up through a one-way platform also touches it; only a player on top and not rising has landed.
            if (collision.collider.bounds.min.y < _col.bounds.max.y - 0.1f) return;
            if (collision.rigidbody != null && collision.rigidbody.linearVelocity.y > 0.1f) return;

            _state = State.Idle;
            _zone.HandleLanding(_index);
        }
    }
}
