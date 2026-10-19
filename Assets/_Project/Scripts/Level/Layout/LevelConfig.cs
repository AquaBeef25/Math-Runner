using MathRunner.Math;
using UnityEngine;

namespace MathRunner.Level
{
    // Everything that makes one level different: its math, pacing and layout. Seeded from LevelCurriculum, then tuned here.
    [CreateAssetMenu(menuName = "MathRunner/Level Config", fileName = "Level00")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _levelNumber = 1;
        [SerializeField] private string _topic = "Addition";
        [SerializeField] private QuestionSettings _questions = new QuestionSettings();

        [Header("Pacing")]
        [SerializeField, Min(1)] private int _questionCount = 8;
        [SerializeField, Min(1f)] private float _runSpeed = 6f;
        [Tooltip("Share of questions with a visible countdown (0 = none, 1 = all). Spread evenly; the first question is untimed unless this is 1.")]
        [SerializeField, Range(0f, 1f)] private float _timedRatio;
        [Tooltip("Seconds to answer a timed question (= run-up time to the platforms).")]
        [SerializeField, Min(0f)] private float _timeLimit;
        [Tooltip("Run-up seconds for untimed questions. Never shorter than the time limit.")]
        [SerializeField, Min(1f)] private float _untimedLeadSeconds = 6f;

        [Header("Layout")]
        [SerializeField, Min(0)] private int _hazardsBetweenQuestions = 1;
        [Tooltip("Chance that a hazard is a gap instead of a spike.")]
        [SerializeField, Range(0f, 1f)] private float _gapChance;
        [SerializeField] private int _layoutSeed = 1;

        [Header("World shape")]
        [Tooltip("0 = flat ground; 1–2 = steps and pillars up to that many tiles.")]
        [SerializeField, Range(0, 2)] private int _maxStepTiles = 1;
        [Tooltip("High/low route splits per level.")]
        [SerializeField, Min(0)] private int _routeSplits = 1;
        [Tooltip("Chance a spike gets an optional platform over it.")]
        [SerializeField, Range(0f, 1f)] private float _platformChance = 0.3f;
        [Tooltip("Chance a hazard is a patrolling slime.")]
        [SerializeField, Range(0f, 1f)] private float _walkerChance;
        [Tooltip("Chance a height change is a ramp instead of a step.")]
        [SerializeField, Range(0f, 1f)] private float _rampChance;
        [Tooltip("Chance a pit is filled with water.")]
        [SerializeField, Range(0f, 1f)] private float _waterChance;
        [Tooltip("Chance a hazard is a saw.")]
        [SerializeField, Range(0f, 1f)] private float _sawChance;
        [SerializeField, Range(0f, 1f)] private float _beeChance;
        [SerializeField, Range(0f, 1f)] private float _coinChance = 0.5f;
        [Tooltip("Leave spikes out of this level (the spots they'd take stay plain ground).")]
        [SerializeField] private bool _noSpikes;
        [Tooltip("Tiles for the ground, ramps and ledges (grass, sand...). Empty = the prefabs' grass.")]
        [SerializeField] private TerrainSet _terrain;
        [Tooltip("Harmless moving life per 10 units of ground (about 0.6 = one per screen).")]
        [SerializeField, Min(0f)] private float _lifeDensity = 0.6f;
        [Tooltip("Harmless moving life for this world (critters, flyers, flags...). Empty = none.")]
        [SerializeField] private LifeSet _life;
        [Tooltip("Relative chance of each encounter enemy: Alien, Bee, Frog (retired, ignored), Fly. Empty = always the alien.")]
        [SerializeField] private float[] _enemyWeights = { 1f, 1f, 1f, 1f };

        [Header("Look")]
        [SerializeField] private BackgroundTheme _background;
        [SerializeField] private DecorationSet _decorations;
        [Tooltip("Decorations per 10 units of ground.")]
        [SerializeField, Min(0f)] private float _decorationDensity = 2.5f;

        public int LevelNumber => _levelNumber;
        public string Topic => _topic;
        public QuestionSettings Questions => _questions;
        public int QuestionCount => _questionCount;
        public float RunSpeed => _runSpeed;
        public float TimedRatio => _timedRatio;
        public float TimeLimit => _timeLimit;
        public float UntimedLeadSeconds => Mathf.Max(_untimedLeadSeconds, _timeLimit);
        public int HazardsBetweenQuestions => _hazardsBetweenQuestions;
        public float GapChance => _gapChance;
        public int LayoutSeed => _layoutSeed;
        public int MaxStepTiles => _maxStepTiles;
        public int RouteSplits => _routeSplits;
        public float PlatformChance => _platformChance;
        public float WalkerChance => _walkerChance;
        public float SawChance => _sawChance;
        public float BeeChance => _beeChance;
        public float CoinChance => _coinChance;
        public bool NoSpikes => _noSpikes;
        public TerrainSet Terrain => _terrain;
        public float LifeDensity => _lifeDensity;
        public LifeSet Life => _life;
        public float[] EnemyWeights => _enemyWeights;
        public void SetLife(LifeSet life) => _life = life;
        // Editor setup: ScriptableObject fields don't save reliably through SerializedProperty here.
        public void SetTerrain(TerrainSet terrain) => _terrain = terrain;
        public float RampChance => _rampChance;
        public float WaterChance => _waterChance;
        public BackgroundTheme Background => _background;
        public DecorationSet Decorations => _decorations;
        public float DecorationDensity => _decorationDensity;

        // Editor setup: ScriptableObject fields don't always save through SerializedProperty here.
        public void SetDecorations(DecorationSet set) => _decorations = set;

        public void Initialize(int levelNumber, string topic, QuestionSettings questions, int questionCount, float runSpeed,
            float timedRatio, float timeLimit, int hazardsBetweenQuestions, float gapChance)
        {
            _levelNumber = levelNumber;
            _topic = topic;
            _questions = questions;
            _questionCount = questionCount;
            _runSpeed = runSpeed;
            _timedRatio = timedRatio;
            _timeLimit = timeLimit;
            _hazardsBetweenQuestions = hazardsBetweenQuestions;
            _gapChance = gapChance;
            _layoutSeed = levelNumber;
        }

        private void OnValidate()
        {
            var errors = _questions.Validate();
            if (errors.Count > 0) Debug.LogWarning($"{name}: {string.Join(" ", errors)}", this);
            if (_timedRatio > 0f && _timeLimit <= 0f) Debug.LogWarning($"{name}: timed questions need a time limit above 0.", this);
        }
    }
}
