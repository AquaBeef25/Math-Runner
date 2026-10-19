using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    // A row of Kenney hearts, full from the left.
    public class HeartRow : MonoBehaviour
    {
        [SerializeField] private Image[] _hearts = new Image[0];
        [SerializeField] private Sprite _full;
        [SerializeField] private Sprite _empty;

        // Editor setup and tests.
        public void SetImages(Image[] hearts, Sprite full, Sprite empty)
        {
            _hearts = hearts;
            _full = full;
            _empty = empty;
        }

        public void Show(int lives) => Apply(_hearts, _full, _empty, lives);

        // true = full, filled from the left; count clamped to 0..count.
        public static bool[] States(int filled, int count)
        {
            var states = new bool[count];
            for (int i = 0; i < count; i++) states[i] = i < filled;
            return states;
        }

        // Shared with StarRow.
        public static void Apply(Image[] images, Sprite full, Sprite empty, int filled)
        {
            var states = States(filled, images.Length);
            for (int i = 0; i < images.Length; i++)
                if (images[i] != null) images[i].sprite = states[i] ? full : empty;
        }
    }
}
