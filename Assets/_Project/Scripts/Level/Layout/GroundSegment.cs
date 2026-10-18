using System.Collections.Generic;
using UnityEngine;

namespace MathRunner.Level
{
    // A solid block of ground or wall: one tile row of grass on top, dirt below. The builder sizes it.
    // Each side is outlined (end cap tiles) only where it is exposed: above a lower neighbour's top, all the way down over a
    // gap, and not at all against a taller neighbour or where a ramp continues it. So steps and pillars read as one piece
    // of ground instead of separate outlined boxes.
    [RequireComponent(typeof(BoxCollider2D))]
    public class GroundSegment : MonoBehaviour
    {
        [SerializeField] private TiledStrip _top;
        [SerializeField] private TiledStrip _fill;
        [SerializeField] private float _topHeight = 1f;

        private readonly List<TiledStrip> _bands = new List<TiledStrip>();

        // The dirt below the grass row, split where a side's outline starts or stops (bottom band first).
        public IReadOnlyList<TiledStrip> FillBands => _bands;

        // Height above which a side is outlined, given the neighbour's top on that side (null = a gap / open edge).
        public static float EdgeFrom(float top, float? neighbourTop) =>
            neighbourTop == null ? float.NegativeInfinity
            : neighbourTop.Value >= top - 0.001f ? float.PositiveInfinity
            : neighbourTop.Value;

        public void SetRect(float left, float bottom, float right, float top, bool capLeft = true, bool capRight = true) =>
            SetRect(left, bottom, right, top, capLeft ? float.NegativeInfinity : float.PositiveInfinity,
                capRight ? float.NegativeInfinity : float.PositiveInfinity);

        public void SetRect(float left, float bottom, float right, float top, float leftEdgeFrom, float rightEdgeFrom)
        {
            float width = right - left;
            float height = top - bottom;
            transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
            transform.localScale = Vector3.one;
            GetComponent<BoxCollider2D>().size = new Vector2(width, height);
            float centerY = (bottom + top) * 0.5f;

            float topRow = Mathf.Min(_topHeight, height);
            _top.SetCaps(Exposed(top - topRow, leftEdgeFrom), Exposed(top - topRow, rightEdgeFrom));
            _top.transform.localPosition = new Vector3(0f, height * 0.5f - topRow * 0.5f, 0f);
            _top.SetSize(width, topRow);

            // Dirt bands between the bottom, the outline starts that fall inside the dirt, and the grass row.
            float fillTop = top - topRow;
            var cuts = new List<float> { bottom, fillTop };
            foreach (float edge in new[] { leftEdgeFrom, rightEdgeFrom })
                if (edge > bottom + 0.001f && edge < fillTop - 0.001f && !cuts.Contains(edge)) cuts.Add(edge);
            cuts.Sort();

            var spans = new List<(float B0, float B1)>();
            for (int i = 0; i + 1 < cuts.Count; i++)
                if (cuts[i + 1] - cuts[i] > 0.001f) spans.Add((cuts[i], cuts[i + 1]));

            // Clone the extra bands before any caps change: a clone doesn't carry the strip's remembered cap tiles.
            _bands.Clear();
            _fill.gameObject.SetActive(spans.Count > 0);
            for (int i = 0; i < spans.Count; i++) _bands.Add(i == 0 ? _fill : Instantiate(_fill, transform));
            for (int i = 0; i < spans.Count; i++)
            {
                var (b0, b1) = spans[i];
                var band = _bands[i];
                band.SetCaps(Exposed(b0, leftEdgeFrom), Exposed(b0, rightEdgeFrom));
                band.transform.localPosition = new Vector3(0f, (b0 + b1) * 0.5f - centerY, 0f);
                band.SetSize(width, b1 - b0);
            }
        }

        // Editor setup and tests.
        public void SetStrips(TiledStrip top, TiledStrip fill)
        {
            _top = top;
            _fill = fill;
        }

        private static bool Exposed(float bandBottom, float edgeFrom) => bandBottom >= edgeFrom - 0.001f;
    }
}
