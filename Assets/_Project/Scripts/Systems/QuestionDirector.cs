using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Math;
using UnityEngine;

namespace MathRunner.Systems
{
    // Owns the level's question generator and adaptive difficulty; Question Zones ask it for the next question.
    public class QuestionDirector : MonoBehaviour
    {
        [Tooltip("Level whose questions are used. The LevelBuilder overrides this at runtime with the selected level.")]
        [SerializeField] private LevelConfig _config;
        [Tooltip("0 = different questions every run. Any other value repeats the same sequence.")]
        [SerializeField] private int _seed;

        private QuestionGenerator _generator;
        private DifficultyTracker _tracker;

        public DifficultyTier Tier => _tracker.Tier;
        public int Level => _config.LevelNumber;
        public LevelConfig Config => _config;

        private void Awake()
        {
            if (_config != null && _generator == null) Configure(_config);
        }

        private void OnEnable()
        {
            GameEvents.QuestionResolved += OnQuestionResolved;
        }

        private void OnDisable()
        {
            GameEvents.QuestionResolved -= OnQuestionResolved;
        }

        public void Configure(LevelConfig config)
        {
            _config = config;
            var rng = _seed == 0 ? new System.Random() : new System.Random(_seed);
            _generator = new QuestionGenerator(rng);
            _tracker = new DifficultyTracker();
        }

        public MathQuestion NextQuestion() => _generator.Generate(_config.Questions, _tracker.Tier);

        private void OnQuestionResolved(QuestionResult result)
        {
            if (result.Outcome == AnswerOutcome.Correct) _tracker.RecordCorrect();
            else _tracker.RecordWrong();
        }
    }
}
