using MathRunner.Core;
using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Level
{
    [RequireComponent(typeof(Collider2D))]
    public class FinishFlag : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerController _)) GameEvents.RaiseLevelCompleted();
        }
    }
}
