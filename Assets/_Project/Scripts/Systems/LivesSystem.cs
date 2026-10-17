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
            GameEvents.QuestionResolved += OnQuestionResolved;
            GameEvents.PlayerHit += OnPlayerHit;
        }

        private void OnDisable()
        {
            GameEvents.QuestionResolved -= OnQuestionResolved;
            GameEvents.PlayerHit -= OnPlayerHit;
        }

        private void Start()
        {
            GameEvents.RaiseLivesChanged(Lives);
        }

        private void OnQuestionResolved(QuestionResult result)
        {
            if (result.Outcome != AnswerOutcome.Correct) LoseLife();
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
