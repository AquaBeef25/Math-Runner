using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Level
{
    // Mud on a fork's route, shown only when that route was a wrong answer: the runner slows down while in it (the
    // heart itself is taken by the ForkZone when the route is chosen). Back to full speed on leaving it.
    [RequireComponent(typeof(SpriteRenderer))]
    public class MudPatch : MonoBehaviour
    {
        public const float SlowFactor = 0.5f;
        private const float Depth = 0.35f;

        private bool _armed;

        public bool IsArmed => _armed;

        // Mud lying on the surface at y from left to right (tiled sprite), hidden and harmless until armed.
        public static MudPatch Create(Transform parent, float left, float right, float y, Sprite sprite, Color tint)
        {
            var go = new GameObject("Mud");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((left + right) * 0.5f, y + Depth * 0.5f - 0.08f, 0f); // lies on the surface
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = 3;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(right - left, Depth);
            sr.enabled = false;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(right - left, 1.5f);
            box.offset = new Vector2(0f, 0.6f);
            box.enabled = false;
            return go.AddComponent<MudPatch>();
        }

        public void Arm()
        {
            _armed = true;
            GetComponent<SpriteRenderer>().enabled = true;
            GetComponent<BoxCollider2D>().enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerController controller)) Enter(controller, controller.GetComponent<PlayerHealth>());
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerController controller)) Exit(controller);
        }

        public void Enter(PlayerController controller, PlayerHealth health)
        {
            if (!_armed) return;
            controller.SpeedFactor = SlowFactor;
        }

        public void Exit(PlayerController controller)
        {
            if (_armed) controller.SpeedFactor = 1f;
        }
    }
}
