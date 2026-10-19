using System.Collections;
using MathRunner.Core;
using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Level
{
    // The reward at the end of a fork's right route: once armed (the route was the right answer), touching it opens it
    // and pops its coins, each adding its value to the score like a normal coin. A chest on a wrong route stays shut.
    [RequireComponent(typeof(SpriteRenderer))]
    public class TreasureChest : MonoBehaviour
    {
        private const float Size = 1.1f;
        private const float PopSeconds = 0.6f;

        [SerializeField] private Sprite _open;
        [SerializeField] private Sprite _coin;
        [SerializeField, Min(0)] private int _coins = 5;
        [SerializeField, Min(0)] private int _value = 10;

        private bool _armed;
        private bool _opened;

        public bool IsArmed => _armed;
        public bool IsOpened => _opened;

        public static TreasureChest Create(Transform parent, Vector3 feet, Sprite closed, Sprite open, Sprite coin, int coins, int value)
        {
            var go = new GameObject("Chest");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = closed;
            sr.sortingOrder = 4;
            if (closed != null)
            {
                float scale = Size / Mathf.Max(0.01f, closed.bounds.size.x);
                go.transform.localScale = Vector3.one * scale;
                go.transform.position += Vector3.up * (-closed.bounds.min.y * scale); // sits on the surface
            }
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.2f, 2f) / go.transform.localScale.x;
            box.offset = new Vector2(0f, box.size.y * 0.5f);
            var chest = go.AddComponent<TreasureChest>();
            chest._open = open;
            chest._coin = coin;
            chest._coins = coins;
            chest._value = value;
            return chest;
        }

        public void Arm() => _armed = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerController _)) Touch();
        }

        public void Touch()
        {
            if (!_armed || _opened) return;
            _opened = true;
            if (_open != null) GetComponent<SpriteRenderer>().sprite = _open;
            for (int i = 0; i < _coins; i++) GameEvents.RaiseCoinCollected(_value);
            if (Application.isPlaying && _coin != null) StartCoroutine(PopCoins());
        }

        // Coins burst up in a little fan and fade (looks only; the score was already counted).
        private IEnumerator PopCoins()
        {
            var coins = new Transform[_coins];
            for (int i = 0; i < _coins; i++)
            {
                var go = new GameObject("ChestCoin");
                go.transform.position = transform.position + Vector3.up * 0.8f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _coin;
                sr.sortingOrder = 6;
                coins[i] = go.transform;
            }
            for (float t = 0f; t < PopSeconds; t += Time.deltaTime)
            {
                float k = t / PopSeconds;
                for (int i = 0; i < coins.Length; i++)
                {
                    float spread = _coins > 1 ? (i / (float)(_coins - 1) - 0.5f) * 2.4f : 0f;
                    coins[i].position = transform.position + new Vector3(spread * k, 0.8f + 2.2f * k - 1.2f * k * k, 0f);
                    coins[i].GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f - k * k);
                }
                yield return null;
            }
            foreach (var c in coins) Destroy(c.gameObject);
        }
    }
}
