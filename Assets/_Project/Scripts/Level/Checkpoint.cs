using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Level
{
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerHealth health)) health.SetCheckpoint(transform.position);
        }
    }
}
