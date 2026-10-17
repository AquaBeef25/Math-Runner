using UnityEngine;
using MathRunner.Player;

namespace MathRunner.Level
{
    // Phase 1 sandbox helper: teleports the player back to the start after falling. Phase 4 replaces this with losing a life.
    [RequireComponent(typeof(BoxCollider2D))]
    public class FallResetZone : MonoBehaviour
    {
        [SerializeField] private Transform _respawnPoint;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerController player))
            {
                player.Respawn(_respawnPoint.position);
            }
        }
    }
}
