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
    }
}
