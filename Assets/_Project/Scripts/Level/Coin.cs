using System.Collections;
using MathRunner.Core;
using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Level
{
    // A gold coin: touching it adds its value to the score (never to the stars), then it pops up and fades out.
    [RequireComponent(typeof(Collider2D))]
    public class Coin : MonoBehaviour
    {
        [SerializeField] private int _value = 10;
        [SerializeField] private float _popSeconds = 0.25f;

        private bool _taken;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken || !other.TryGetComponent(out PlayerController _)) return;
            _taken = true;
            GetComponent<Collider2D>().enabled = false;
            GameEvents.RaiseCoinCollected(_value);
            StartCoroutine(Pop());
        }

        private IEnumerator Pop()
        {
            var sprite = GetComponentInChildren<SpriteRenderer>();
            Vector3 start = transform.position;
            for (float t = 0f; t < _popSeconds; t += Time.deltaTime)
            {
                float k = t / _popSeconds;
                transform.position = start + Vector3.up * (0.6f * k);
                if (sprite != null) sprite.color = new Color(1f, 1f, 1f, 1f - k);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
