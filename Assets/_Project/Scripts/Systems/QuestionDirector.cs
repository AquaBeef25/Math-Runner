using MathRunner.Core;
using MathRunner.Math;
using UnityEngine;

namespace MathRunner.Systems
{
    // Owns the level's question generator and adaptive difficulty; Question Zones ask it for the next question.
    public class QuestionDirector : MonoBehaviour
    {
        [SerializeField, Range(1, LevelCurriculum.LevelCount)] private int _level = 1;
        [Tooltip("0 = different questions every run. Any other value repeats the same sequence.")]
        [SerializeField] private int _seed;

        private QuestionSettings _settings;
        private QuestionGenerator _generator;
        private DifficultyTracker _tracker;

        public DifficultyTier Tier => _tracker.Tier;
        public int Level => _level;

        private void Awake()
        {
            _settings = LevelCurriculum.ForLevel(_level);
            var rng = _seed == 0 ? new System.Random() : new System.Random(_seed);
            _generator = new QuestionGenerator(rng);
            _tracker = new DifficultyTracker();
        }

        private void OnEnable()
        {
            GameEvents.QuestionResolved += OnQuestionResolved;
        }

        private void OnDisable()
        {
            GameEvents.QuestionResolved -= OnQuestionResolved;
        }

        public MathQuestion NextQuestion() => _generator.Generate(_settings, _tracker.Tier);

        private void OnQuestionResolved(QuestionResult result)
        {
            if (result.Outcome == AnswerOutcome.Correct) _tracker.RecordCorrect();
            else _tracker.RecordWrong();
        }
    }
}
