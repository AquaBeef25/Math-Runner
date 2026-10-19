using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    // Kenney stars for a level's rating (0-3), full from the left.
    public class StarRow : MonoBehaviour
    {
        [SerializeField] private Image[] _stars = new Image[0];
        [SerializeField] private Sprite _full;
        [SerializeField] private Sprite _empty;

        public int Stars { get; private set; }

        // Editor setup and tests.
        public void SetImages(Image[] stars, Sprite full, Sprite empty)
        {
            _stars = stars;
            _full = full;
            _empty = empty;
        }

        public void Show(int stars)
        {
            Stars = Mathf.Clamp(stars, 0, _stars.Length);
            HeartRow.Apply(_stars, _full, _empty, Stars);
        }
    }
}
