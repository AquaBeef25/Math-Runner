using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Level
{
    // Trigger hazard (e.g. spikes): costs a life but doesn't block the run.
    [RequireComponent(typeof(Collider2D))]
    public class Obstacle : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerHealth health)) health.HitByObstacle();
        }
    }
}
