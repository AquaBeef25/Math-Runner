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
        [Tooltip("Optional tile with both end caps, drawn alone when the strip is only one cap wide (e.g. a 1-tile pillar).")]
        [SerializeField] private Sprite _single;

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

        // Centered on this transform. One cap wide with a single tile set: that tile alone. Narrower than two caps: the
        // caps keep their full width and overlap in the middle (a Tiled sprite crops instead of shrinking, so a narrower
        // cap would lose its outer edge); only below one cap width do they shrink.
        public void SetSize(float width, float height)
        {
            bool capsShown = (_leftCapSprite == null || _left.sprite == _leftCapSprite) && (_rightCapSprite == null || _right.sprite == _rightCapSprite);
            bool single = _single != null && capsShown && width <= _capWidth + 0.001f;
            _left.enabled = _right.enabled = !single;
            if (single)
            {
                if (_middleSprite == null) _middleSprite = _middle.sprite;
                _middle.sprite = _single;
                _middle.enabled = true;
                Place(_middle, 0f, width, height);
                return;
            }
            if (_middleSprite != null) _middle.sprite = _middleSprite;

            float cap = width >= _capWidth ? _capWidth : width * 0.5f;
            float middle = width - 2f * cap;
            Place(_left, -width * 0.5f + cap * 0.5f, cap, height);
            Place(_right, width * 0.5f - cap * 0.5f, cap, height);
            _middle.enabled = middle > 0.001f;
            if (_middle.enabled) Place(_middle, 0f, middle, height);
        }

        // Editor setup and tests.
        public void SetPieces(SpriteRenderer left, SpriteRenderer middle, SpriteRenderer right)
        {
            _left = left;
            _middle = middle;
            _right = right;
        }

        public void SetSingle(Sprite single) => _single = single;

        private Sprite _leftCapSprite;
        private Sprite _rightCapSprite;
        private Sprite _middleSprite;

        // Re-skin the strip (e.g. bridge planks or "!" blocks) with the same layout.
        public void SetSprites(Sprite left, Sprite middle, Sprite right)
        {
            _left.sprite = _leftCapSprite = left;
            _middle.sprite = _middleSprite = middle;
            _right.sprite = _rightCapSprite = right;
            _single = null; // a re-skinned strip (bridge, blocks) has no single tile of its own
        }

        // Hide an end cap (draw a middle tile there instead) where the strip continues seamlessly, e.g. into a ramp.
        public void SetCaps(bool left, bool right)
        {
            if (_leftCapSprite == null) _leftCapSprite = _left.sprite;
            if (_rightCapSprite == null) _rightCapSprite = _right.sprite;
            if (_middleSprite == null) _middleSprite = _middle.sprite;
            _left.sprite = left ? _leftCapSprite : _middleSprite;
            _right.sprite = right ? _rightCapSprite : _middleSprite;
        }

        private static void Place(SpriteRenderer piece, float x, float width, float height)
        {
            piece.transform.localPosition = new Vector3(x, 0f, 0f);
            piece.size = new Vector2(width, height);
        }
    }
}
