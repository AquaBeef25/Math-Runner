using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Level
{
    // Water filling a pit. Falling in counts like falling into the pit: one heart, then back to the last checkpoint.
    [RequireComponent(typeof(BoxCollider2D))]
    public class WaterPool : MonoBehaviour
    {
        [SerializeField] private Sprite _surface;   // water_top
        [SerializeField] private Sprite _body;      // water
        [SerializeField] private int _sortingOrder = 1;

        public void SetRect(float left, float right, float surface, float bottom)
        {
            float width = right - left;
            transform.position = new Vector3((left + right) * 0.5f, 0f, 0f);
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, surface - bottom);
            col.offset = new Vector2(0f, (surface + bottom) * 0.5f);

            if (_surface == null) return; // collider only (tests)
            Strip("Surface", _surface, surface - 0.5f, width, 1f);
            if (surface - 1f - bottom > 0.01f) Strip("Body", _body, (surface - 1f + bottom) * 0.5f, width, surface - 1f - bottom);
        }

        private void Strip(string name, Sprite sprite, float y, float width, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height);
            sr.sortingOrder = _sortingOrder;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerHealth health)) health.Fell();
        }
    }
}
