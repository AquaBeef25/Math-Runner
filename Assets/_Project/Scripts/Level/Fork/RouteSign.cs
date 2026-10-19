using TMPro;
using UnityEngine;

namespace MathRunner.Level
{
    // A fork route's answer, floating at the route's entrance: a big chalk number on a dark chalkboard disc, ringed in the
    // route's colour with a soft pulsing glow of the same colour, so it is easy to spot from far away. The board shows
    // no answer circles at a fork, so these signs carry the answers.
    public class RouteSign : MonoBehaviour
    {
        public const float Size = 1.3f;
        private const float GlowScale = 2.3f;
        private static readonly Color DiscColor = new Color32(0x2B, 0x54, 0x43, 0xF2);
        private static Sprite _softCircle;
        private static Sprite _solidCircle;

        [SerializeField] private TextMeshPro _text;
        [SerializeField] private SpriteRenderer _glow;

        private Color _glowColor;

        public float Diameter => Size;
        public SpriteRenderer Glow => _glow;

        public static RouteSign Create(Transform parent, Vector3 centre, Sprite ring, TMP_FontAsset font, Color color)
        {
            var go = new GameObject("RouteSign");
            go.transform.SetParent(parent, false);
            go.transform.position = centre;

            var glow = Part(go.transform, "Glow", SoftCircle(), Vivid(color, 0.85f), 1, Size * GlowScale);
            Part(go.transform, "Disc", SolidCircle(), DiscColor, 2, Size);
            if (ring != null)
            {
                var r = Part(go.transform, "Ring", ring, color, 3, 1f);
                float fit = Size * 1.08f / Mathf.Max(0.01f, Mathf.Max(ring.bounds.size.x, ring.bounds.size.y));
                r.transform.localScale = Vector3.one * fit;
            }

            var label = new GameObject("Answer");
            label.transform.SetParent(go.transform, false);
            var text = label.AddComponent<TextMeshPro>();
            if (font != null) text.font = font;
            text.fontSize = 8f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(Size * 1.5f, Size);
            text.sortingOrder = 4;

            var sign = go.AddComponent<RouteSign>();
            sign._text = text;
            sign._glow = glow;
            sign._glowColor = glow.color;
            return sign;
        }

        public void Show(int answer)
        {
            _text.text = answer.ToString();
            _text.ForceMeshUpdate();
        }

        // A slow breathing glow (looks only, real time so it keeps pulsing in slow motion).
        private void Update()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
            _glow.transform.localScale = Vector3.one * (Size * GlowScale * (0.92f + 0.16f * pulse));
            var c = _glowColor;
            c.a = 0.6f + 0.35f * pulse;
            _glow.color = c;
        }

        // The pale chalk colour pushed towards full saturation so the halo stands out against a light sky.
        private static Color Vivid(Color color, float alpha)
        {
            Color.RGBToHSV(color, out float h, out float sat, out float v);
            var c = Color.HSVToRGB(h, Mathf.Max(sat, 0.75f), Mathf.Max(v, 0.95f));
            c.a = alpha;
            return c;
        }

        private static SpriteRenderer Part(Transform parent, string name, Sprite sprite, Color color, int order, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        // 1-unit circles made once at runtime (no art needed): a soft glow fading to the edge, and a solid disc.
        private static Sprite SoftCircle() => _softCircle != null ? _softCircle : _softCircle = Circle("SoftCircle", true);
        private static Sprite SolidCircle() => _solidCircle != null ? _solidCircle : _solidCircle = Circle("SolidCircle", false);

        private static Sprite Circle(string name, bool soft)
        {
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N * 0.5f, N * 0.5f)) / (N * 0.5f);
                    float a = soft ? Mathf.Pow(Mathf.Clamp01(1f - d), 1.3f) : Mathf.Clamp01((1f - d) * N * 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            sprite.name = name;
            return sprite;
        }
    }
}
