using UnityEngine;

namespace MathRunner.Level
{
    // Builds the parallax layers of a BackgroundTheme as children. A level applies its own theme; scenes without a level
    // (sandboxes) fall back to the default theme.
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private BackgroundTheme _defaultTheme;

        private bool _applied;

        // Editor setup: ScriptableObject scene fields don't always save through SerializedProperty here.
        public void SetDefaultTheme(BackgroundTheme theme) => _defaultTheme = theme;

        private void Start()
        {
            if (!_applied) Apply(_defaultTheme);
        }

        public void Apply(BackgroundTheme theme)
        {
            if (theme == null) return;
            _applied = true;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child); // Edit-mode previews
            }

            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = theme.SkyColor;

            for (int i = 0; i < theme.Layers.Count; i++)
            {
                var layer = theme.Layers[i];
                var go = new GameObject("Layer " + i + " " + (layer.Sprite != null ? layer.Sprite.name : ""));
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = layer.Sprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                float height = layer.Size.y > 0f || layer.Sprite == null ? layer.Size.y : layer.Sprite.bounds.size.y;
                sr.size = new Vector2(layer.Size.x, height);
                sr.sortingOrder = layer.Order;
                go.AddComponent<ParallaxLayer>().Configure(layer.Follow, layer.Anchor, layer.TileWidth);
            }
        }
    }
}
