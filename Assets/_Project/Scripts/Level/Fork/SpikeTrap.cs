using UnityEngine;

namespace MathRunner.Level
{
    // Spikes on a fork's route, shown only when that route was a wrong answer: they pop up out of the ground the moment
    // the route is chosen. Looks only (no collider): the heart was already taken by the ForkZone at that moment, so
    // running over them or jumping them changes nothing.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpikeTrap : MonoBehaviour
    {
        public const float SinkDepth = 0.9f;
        public const float RiseSeconds = 0.18f;
        private const float Height = 1f;

        private bool _armed;
        private float _riseTime;
        private float _surfaceY;

        public bool IsArmed => _armed;

        // How far below its standing place the row is, `t` seconds after popping (eased out, then 0 for good).
        public static float RiseOffset(float t)
        {
            float k = Mathf.Clamp01(t / RiseSeconds);
            k = 1f - (1f - k) * (1f - k);
            return -SinkDepth * (1f - k);
        }

        // A row of spikes standing on the surface at y from left to right (tiled sprite), hidden until armed.
        public static SpikeTrap Create(Transform parent, float left, float right, float y, Sprite sprite)
        {
            var go = new GameObject("Spikes");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -1; // behind the ground, so they come up out of it
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(right - left, Height);
            sr.enabled = false;
            var trap = go.AddComponent<SpikeTrap>();
            trap._surfaceY = y + Height * 0.5f;
            go.transform.position = new Vector3((left + right) * 0.5f, trap._surfaceY + RiseOffset(0f), 0f);
            return trap;
        }

        public void Arm()
        {
            if (_armed) return;
            _armed = true;
            _riseTime = 0f;
            GetComponent<SpriteRenderer>().enabled = true;
        }

        private void Update()
        {
            if (!_armed || _riseTime > RiseSeconds) return;
            _riseTime += Time.deltaTime;
            var p = transform.position;
            transform.position = new Vector3(p.x, _surfaceY + RiseOffset(_riseTime), p.z);
        }
    }
}
