using UnityEngine;

namespace MathRunner.Level
{
    // A sloped piece of ground (2 units of run per tile of rise): a polygon collider following the slope, drawn with the
    // Kenney long-ramp tiles (mirrored for an upward slope) and filled with dirt below. Also works flat (from == to).
    [RequireComponent(typeof(PolygonCollider2D))]
    public class RampPiece : MonoBehaviour
    {
        [SerializeField] private Sprite _highHalf;   // terrain_grass_ramp_long_a: surface from 1 down to 0.5 within its tile
        [SerializeField] private Sprite _lowHalf;    // terrain_grass_ramp_long_b: surface from 0.5 down to 0 (grass wedge only)
        [SerializeField] private Sprite _underLow;   // terrain_grass_ramp_long_c: dirt under the low half, where its grass band ends
        [SerializeField] private Sprite _fill;       // terrain_grass_block_center
        [SerializeField] private Sprite _flatTop;    // terrain_grass_block_top (for flat pieces)
        [SerializeField] private int _sortingOrder;

        public void SetRamp(float start, float end, float fromTop, float toTop, float bottom)
        {
            transform.position = new Vector3(start, 0f, 0f);
            float length = end - start;
            var col = GetComponent<PolygonCollider2D>();
            col.pathCount = 1;
            // The start slants back under the flat ground it joins (hidden inside that block) instead of rising as a vertical
            // face to the surface: the runner's box snagged on that hidden corner and stopped dead.
            const float Undercut = 0.5f;
            col.SetPath(0, new[]
            {
                new Vector2(0f, bottom), new Vector2(-Undercut, fromTop - Undercut), new Vector2(0f, fromTop),
                new Vector2(length, toTop), new Vector2(length, bottom),
            });

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            if (_fill == null) return; // collider-only (tests)

            int columns = Mathf.Max(1, Mathf.RoundToInt(length));
            float slope = (toTop - fromTop) / length;
            for (int c = 0; c < columns; c++)
            {
                float h0 = fromTop + slope * c, h1 = fromTop + slope * (c + 1);
                float low = Mathf.Min(h0, h1);
                int row = Mathf.FloorToInt(low + 1e-3f);
                float cx = c + 0.5f;
                if (Mathf.Approximately(h0, h1))
                {
                    Tile(_flatTop, cx, h0 - 0.5f, false);
                    Fill(cx, bottom, h0 - 1f);
                    continue;
                }
                // Within its tile row the surface is either the high half (1 → 0.5) or the low half (0.5 → 0) of a ramp.
                bool lowHalf = Mathf.Abs(low - row) < 1e-3f;
                bool rising = h1 > h0;
                Tile(lowHalf ? _lowHalf : _highHalf, cx, row + 0.5f, rising);
                if (lowHalf && _underLow != null)
                {
                    Tile(_underLow, cx, row - 0.5f, rising);
                    Fill(cx, bottom, row - 1);
                }
                else Fill(cx, bottom, row);
            }
        }

        // Editor setup and tests.
        public void SetSprites(Sprite highHalf, Sprite lowHalf, Sprite underLow, Sprite fill, Sprite flatTop)
        {
            _highHalf = highHalf;
            _lowHalf = lowHalf;
            _underLow = underLow;
            _fill = fill;
            _flatTop = flatTop;
        }

        private void Tile(Sprite sprite, float x, float y, bool flip)
        {
            var go = new GameObject(sprite != null ? sprite.name : "Tile");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.flipX = flip;
            sr.sortingOrder = _sortingOrder;
        }

        private void Fill(float x, float bottom, float top)
        {
            if (top - bottom < 0.01f) return;
            var go = new GameObject("Fill");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(x, (bottom + top) * 0.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _fill;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(1f, top - bottom);
            sr.sortingOrder = _sortingOrder;
        }
    }
}
