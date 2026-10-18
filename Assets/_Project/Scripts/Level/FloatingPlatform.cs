using UnityEngine;

namespace MathRunner.Level
{
    // A floating grass ledge the player can jump up through and land on (one-way, via PlatformEffector2D).
    [RequireComponent(typeof(BoxCollider2D))]
    public class FloatingPlatform : MonoBehaviour
    {
        [SerializeField] private TiledStrip _strip;
        [Tooltip("Collider thickness under the walkable top.")]
        [SerializeField, Min(0.05f)] private float _thickness = 0.4f;
        [SerializeField, Min(0.1f)] private float _visualHeight = 1f;

        [Header("Styles")]
        [SerializeField] private Sprite _bridge;       // bridge
        [SerializeField] private Sprite _block;        // block_exclamation
        [SerializeField] private Sprite _pillar;       // terrain_grass_vertical_middle
        [SerializeField] private int _pillarOrder = -3;
        [Tooltip("Pillars stand behind the play area (the runner passes in front of them), so they're drawn darker.")]
        [SerializeField] private Color _pillarTint = new Color(0.62f, 0.68f, 0.74f);

        public void SetRect(float left, float right, float top)
        {
            float width = right - left;
            transform.position = new Vector3((left + right) * 0.5f, top, 0f);
            var col = GetComponent<BoxCollider2D>();
            col.size = new Vector2(width, _thickness);
            col.offset = new Vector2(0f, -_thickness * 0.5f);
            _strip.transform.localPosition = new Vector3(0f, -_visualHeight * 0.5f, 0f);
            _strip.SetSize(width, _visualHeight);
        }

        // Same one-way ledge, different look. A pillar ledge gets a grass column down to the ground below its middle.
        public void SetStyle(PlatformStyle style, float groundTop)
        {
            if (style == PlatformStyle.Bridge && _bridge != null) _strip.SetSprites(_bridge, _bridge, _bridge);
            else if (style == PlatformStyle.Blocks && _block != null) _strip.SetSprites(_block, _block, _block);
            if (style != PlatformStyle.PillarLedge || _pillar == null) return;

            float top = transform.position.y - _visualHeight;
            float height = top - groundTop;
            if (height < 0.05f) return;
            var go = new GameObject("Pillar");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -_visualHeight - height * 0.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _pillar;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(1f, height);
            sr.sortingOrder = _pillarOrder;
            sr.color = _pillarTint;
        }
    }
}
