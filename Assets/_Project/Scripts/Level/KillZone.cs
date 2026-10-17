using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Level
{
    // Placed below gaps: falling in costs a life and respawns the player at the last checkpoint.
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerHealth health)) health.Fell();
        }
    }
}
