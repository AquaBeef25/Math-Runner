using UnityEngine;

namespace MathRunner.Level
{
    // A solid block of ground or wall: one tile row of grass on top, dirt below. The builder sizes it.
    [RequireComponent(typeof(BoxCollider2D))]
    public class GroundSegment : MonoBehaviour
    {
        [SerializeField] private TiledStrip _top;
        [SerializeField] private TiledStrip _fill;
        [SerializeField] private float _topHeight = 1f;

        public void SetRect(float left, float bottom, float right, float top, bool capLeft = true, bool capRight = true)
        {
            _top.SetCaps(capLeft, capRight);
            _fill.SetCaps(capLeft, capRight);
            float width = right - left;
            float height = top - bottom;
            transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
            transform.localScale = Vector3.one;
            GetComponent<BoxCollider2D>().size = new Vector2(width, height);

            float topRow = Mathf.Min(_topHeight, height);
            _top.transform.localPosition = new Vector3(0f, height * 0.5f - topRow * 0.5f, 0f);
            _top.SetSize(width, topRow);

            float fill = height - topRow;
            _fill.gameObject.SetActive(fill > 0.001f);
            if (fill <= 0.001f) return;
            _fill.transform.localPosition = new Vector3(0f, -height * 0.5f + fill * 0.5f, 0f);
            _fill.SetSize(width, fill);
        }
    }
}
