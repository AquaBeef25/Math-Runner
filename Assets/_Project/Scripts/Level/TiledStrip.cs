using UnityEngine;

namespace MathRunner.Level
{
    // A horizontal run of tiles drawn as left cap + tiled middle + right cap (three SpriteRenderers in Tiled draw mode).
    // Only a visual: colliders are sized by their owners, so art never changes the physics.
    public class TiledStrip : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _left;
        [SerializeField] private SpriteRenderer _middle;
        [SerializeField] private SpriteRenderer _right;
        [Tooltip("Width of each end cap in world units (one tile).")]
        [SerializeField] private float _capWidth = 1f;

        public Color Color
        {
            get => _middle.color;
            set
            {
                _left.color = value;
                _middle.color = value;
                _right.color = value;
            }
        }

        // Centered on this transform. Narrower than two caps: the caps shrink to share the width and the middle hides.
        public void SetSize(float width, float height)
        {
            float cap = Mathf.Min(_capWidth, width * 0.5f);
            float middle = width - 2f * cap;

            Place(_left, -width * 0.5f + cap * 0.5f, cap, height);
            Place(_right, width * 0.5f - cap * 0.5f, cap, height);
            _middle.enabled = middle > 0.001f;
            if (_middle.enabled) Place(_middle, 0f, middle, height);
        }

        private Sprite _leftCapSprite;
        private Sprite _rightCapSprite;

        // Re-skin the strip (e.g. bridge planks or "!" blocks) with the same layout.
        public void SetSprites(Sprite left, Sprite middle, Sprite right)
        {
            _left.sprite = _leftCapSprite = left;
            _middle.sprite = middle;
            _right.sprite = _rightCapSprite = right;
        }

        // Hide an end cap (draw a middle tile there instead) where the strip continues seamlessly, e.g. into a ramp.
        public void SetCaps(bool left, bool right)
        {
            if (_leftCapSprite == null) _leftCapSprite = _left.sprite;
            if (_rightCapSprite == null) _rightCapSprite = _right.sprite;
            _left.sprite = left ? _leftCapSprite : _middle.sprite;
            _right.sprite = right ? _rightCapSprite : _middle.sprite;
        }

        private static void Place(SpriteRenderer piece, float x, float width, float height)
        {
            piece.transform.localPosition = new Vector3(x, 0f, 0f);
            piece.size = new Vector2(width, height);
        }
    }
}
