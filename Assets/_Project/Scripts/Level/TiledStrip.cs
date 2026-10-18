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

        private static void Place(SpriteRenderer piece, float x, float width, float height)
        {
            piece.transform.localPosition = new Vector3(x, 0f, 0f);
            piece.size = new Vector2(width, height);
        }
    }
}
