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

        [SerializeField] private TiledStrip _body;
        [Tooltip("Visual height of the platform art; its top lines up with the top of the collider.")]
        [SerializeField] private float _bodyHeight = 1f;
        [SerializeField] private TextMeshPro _label;
        [SerializeField] private float _labelHeight = 0.8f;

        [Header("Feedback")]
        [SerializeField] private Color _idleColor = Color.white;
        [SerializeField] private Color _chosenColor = new Color(1f, 0.9f, 0.45f);
        [SerializeField] private Color _correctColor = new Color(0.45f, 1f, 0.5f);
        [SerializeField] private Color _wrongColor = new Color(1f, 0.45f, 0.45f);
        [SerializeField] private float _crumbleDelay = 0.15f;
        [SerializeField] private float _crumbleDuration = 0.5f;
        [SerializeField] private float _fadedAlpha = 0.3f;

        private BoxCollider2D _col;
        private QuestionZone _zone;
        private int _index;
        private State _state;
        private float _stateTime;
        private bool _chosen;
        private Vector3 _bodyRest;

        private void Awake()
        {
            _col = GetComponent<BoxCollider2D>();
            _col.enabled = false;
            _bodyRest = _body.transform.localPosition;
            _body.Color = _idleColor;
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
            if (_body != null)
            {
                _bodyRest = new Vector3(0f, size.y * 0.5f - _bodyHeight * 0.5f, 0f);
                _body.transform.localPosition = _bodyRest;
                _body.SetSize(size.x, _bodyHeight);
            }
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
            _body.Color = _chosenColor;
            _label.transform.localScale = Vector3.one * 1.2f;
        }

        public void MarkCorrect() => Enter(State.Correct, _correctColor);

        public void Crumble() => Enter(State.Crumbling, _wrongColor);

        public void FadeOut()
        {
            Enter(State.Faded, _body.Color);
            _col.enabled = false;
            SetAlpha(_fadedAlpha);
        }

        private void Enter(State state, Color color)
        {
            _state = state;
            _stateTime = 0f;
            _body.Color = color;
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
                _body.transform.localPosition = _bodyRest + new Vector3(Mathf.Sin(_stateTime * 80f) * 0.06f, 0f, 0f);
                return;
            }

            if (_col.enabled) _col.enabled = false;
            float fall = Mathf.Clamp01((_stateTime - _crumbleDelay) / _crumbleDuration);
            _body.transform.localPosition = _bodyRest + new Vector3(0f, -1.5f * fall * fall, 0f);
            SetAlpha(1f - fall);
            if (fall >= 1f) _state = State.Faded;
        }

        private void SetAlpha(float alpha)
        {
            var c = _body.Color;
            c.a = alpha;
            _body.Color = c;
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
