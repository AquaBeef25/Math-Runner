using UnityEngine;

namespace MathRunner.Level
{
    // A solid block of ground or wall. The builder sizes it; Stage B can swap the visual without touching the builder.
    [RequireComponent(typeof(BoxCollider2D))]
    public class GroundSegment : MonoBehaviour
    {
        public void SetRect(float left, float bottom, float right, float top)
        {
            transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
            transform.localScale = new Vector3(right - left, top - bottom, 1f);
        }
    }
}

