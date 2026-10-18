using Unity.Cinemachine;
using UnityEngine;

namespace MathRunner.Level
{
    // Background band that follows the camera by a fraction of its movement, so it seems farther away than the level.
    // Snaps by whole tile widths to stay under the camera, so a Tiled SpriteRenderer a few tiles wider than the view never runs out.
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("How much of the camera's movement the layer copies per axis: 0 = fixed in the world, 1 = fixed on screen.")]
        [SerializeField] private Vector2 _follow = new Vector2(0.5f, 0.85f);
        [Tooltip("Layer position when the camera is at the world origin.")]
        [SerializeField] private Vector2 _anchor;
        [Tooltip("Width of one repeat of the art in world units; 0 turns horizontal wrapping off.")]
        [SerializeField, Min(0f)] private float _tileWidth = 8f;

        private Transform _camera;

        // Pure so it can be tested: where the layer goes for a given camera position.
        public static Vector2 Position(Vector2 camera, Vector2 follow, Vector2 anchor, float tileWidth)
        {
            float x = anchor.x + camera.x * follow.x;
            if (tileWidth > 0f) x += tileWidth * Mathf.Round((camera.x - x) / tileWidth);
            return new Vector2(x, anchor.y + camera.y * follow.y);
        }

        private void Awake()
        {
            var cam = Camera.main;
            if (cam != null) _camera = cam.transform;
        }

        private void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);

        private void OnDisable() => CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);

        // Cinemachine moves the camera late in the frame; following it from its event avoids a one-frame lag (jitter).
        private void OnCameraUpdated(CinemachineBrain brain) => Refresh();

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (_camera == null) return;
            Vector2 p = Position(_camera.position, _follow, _anchor, _tileWidth);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }
    }
}
