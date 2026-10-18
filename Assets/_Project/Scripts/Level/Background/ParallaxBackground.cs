using UnityEngine;

namespace MathRunner.Level
{
    // Swaps the themed parts of the parallax background. Scenes without a level (sandboxes) keep the theme they were saved with.
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _horizon;
        [SerializeField] private SpriteRenderer _fill;

        public void Apply(BackgroundTheme theme)
        {
            if (theme == null) return;
            // Tiled renderers keep their size, so match the new row's height (rows are cropped to their art, pivot at the top).
            _horizon.sprite = theme.Horizon;
            _horizon.size = new Vector2(_horizon.size.x, theme.Horizon.bounds.size.y);
            _fill.sprite = theme.Fill;
        }
    }
}
