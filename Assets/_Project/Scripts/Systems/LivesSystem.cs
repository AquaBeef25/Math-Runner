using MathRunner.Core;
using UnityEngine;

namespace MathRunner.Systems
{
    public class LivesSystem : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _startingLives = 3;

        public int Lives { get; private set; }
        public int StartingLives => _startingLives;

        private void Awake()
        {
            Lives = _startingLives;
        }

        private void OnEnable()
        {
            // Wrong and missed answers cost their heart through the bullet's PlayerHit, never on the answer itself.
            GameEvents.PlayerHit += OnPlayerHit;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHit -= OnPlayerHit;
        }

        private void Start()
        {
            GameEvents.RaiseLivesChanged(Lives);
        }

        private void OnPlayerHit(HitCause cause) => LoseLife();

        private void LoseLife()
        {
            if (Lives <= 0) return;
            Lives--;
            GameEvents.RaiseLivesChanged(Lives);
            if (Lives == 0) GameEvents.RaiseOutOfLives();
        }
    }
}
