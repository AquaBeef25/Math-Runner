using TMPro;
using UnityEngine;

namespace MathRunner.Level
{
    // A wooden Kenney sign at the start of a fork's route with the route's answer chalked on it, in the same colour as
    // that answer's circle on the board.
    public class RouteSign : MonoBehaviour
    {
        private const float Size = 1f;

        [SerializeField] private TextMeshPro _text;

        public static RouteSign Create(Transform parent, Vector3 feet, Sprite sprite, TMP_FontAsset font, Color color)
        {
            var go = new GameObject("RouteSign");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;
            float scale = sprite != null ? Size / Mathf.Max(0.01f, sprite.bounds.size.y) : 1f;
            go.transform.localScale = Vector3.one * scale;
            if (sprite != null) go.transform.position += Vector3.up * (-sprite.bounds.min.y * scale); // stands on its feet

            var label = new GameObject("Answer");
            label.transform.SetParent(go.transform, false);
            float signHeight = sprite != null ? sprite.bounds.size.y : 1f;
            label.transform.localPosition = new Vector3(0f, signHeight * 0.62f, 0f);
            label.transform.localScale = Vector3.one / scale;
            var text = label.AddComponent<TextMeshPro>();
            if (font != null) text.font = font;
            text.fontSize = 6f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.rectTransform.sizeDelta = new Vector2(2f, 1f);
            text.sortingOrder = 3;

            var sign = go.AddComponent<RouteSign>();
            sign._text = text;
            return sign;
        }

        public void Show(int answer)
        {
            _text.text = answer.ToString();
            _text.ForceMeshUpdate();
        }
    }
}
