using MathRunner.Enemies;
using MathRunner.Player;
using MathRunner.Systems;
using UnityEngine;

namespace MathRunner.Level
{
    // Builds the selected level from prefabs when the Game scene loads.
    public class LevelBuilder : MonoBehaviour
    {
        private const float GroundDepth = 4f;
        private const float WallHeight = 10f;

        [SerializeField] private QuestionDirector _director;
        [SerializeField] private PlayerController _player;
        [Tooltip("Optional: gets the level's background theme.")]
        [SerializeField] private ParallaxBackground _background;

        [SerializeField] private EncounterSettings _encounter;

        [Header("Prefabs")]
        [SerializeField] private GroundSegment _groundPrefab;
        [SerializeField] private EnemyEncounter _encounterPrefab;
        [SerializeField] private Obstacle _spikePrefab;
        [SerializeField] private FloatingPlatform _platformPrefab;
        [SerializeField] private Walker _walkerPrefab;
        [SerializeField] private Saw _sawPrefab;
        [SerializeField, Min(0f)] private float _walkerSpeed = 1f;
        [SerializeField] private Checkpoint _checkpointPrefab;
        [SerializeField] private FinishFlag _finishPrefab;

        public LevelConfig Level { get; private set; }

        // Editor setup: ScriptableObject scene fields don't save reliably through SerializedProperty here.
        public void SetEncounterSettings(EncounterSettings settings) => _encounter = settings;
        public LevelPlan Plan { get; private set; }

        // Without a menu selection (e.g. pressing Play on the Game scene) the first level in the catalog is used.
        private void Awake()
        {
            Level = LevelSelection.Current != null ? LevelSelection.Current : LevelCatalog.Load().Levels[0];
            Build(Level);
        }

        private void Build(LevelConfig config)
        {
            _director.Configure(config);
            _player.RunSpeed = config.RunSpeed;
            if (_background != null) _background.Apply(config.Background);

            var settings = LevelPlanSettings.From(config, _encounter.SpanSeconds, _encounter.FocusScale,
                _player.JumpHeight, Physics2D.gravity.y, _player.BaseGravityScale, _player.FallGravityMultiplier);
            Plan = LevelPlanner.Create(settings);
            var root = transform;

            foreach (var (start, end, top) in Plan.Ground)
                Instantiate(_groundPrefab, root).SetRect(start, -GroundDepth, end, top);
            float endTop = Plan.GroundTopAt(Plan.EndX);
            Instantiate(_groundPrefab, root).SetRect(Plan.EndX, -GroundDepth, Plan.EndX + 2f, endTop + WallHeight);

            foreach (var (x, y) in Plan.Spikes)
                Instantiate(_spikePrefab, new Vector3(x, y, 0f), Quaternion.identity, root);

            foreach (var (left, right, top) in Plan.Platforms)
                Instantiate(_platformPrefab, root).SetRect(left, right, top);

            foreach (var (left, right, y) in Plan.Walkers)
                Instantiate(_walkerPrefab, new Vector3(left, y, 0f), Quaternion.identity, root).Configure(left, right, _walkerSpeed);

            foreach (var (x, y) in Plan.Saws)
                Instantiate(_sawPrefab, new Vector3(x, y, 0f), Quaternion.identity, root);

            foreach (var zonePlan in Plan.Zones)
            {
                var encounter = Instantiate(_encounterPrefab, new Vector3(zonePlan.X, Plan.GroundTopAt(zonePlan.X), 0f), Quaternion.identity, root);
                encounter.Configure(_director, zonePlan.Lead, zonePlan.Timed);
            }

            foreach (var (x, y) in Plan.Checkpoints)
                Instantiate(_checkpointPrefab, new Vector3(x, y, 0f), Quaternion.identity, root);

            Instantiate(_finishPrefab, new Vector3(Plan.FinishX, Plan.GroundTopAt(Plan.FinishX), 0f), Quaternion.identity, root);

            var kill = new GameObject("KillZone");
            kill.transform.SetParent(root, false);
            kill.transform.position = new Vector3((LevelPlanner.StartX + Plan.EndX) * 0.5f, -8f, 0f);
            var trigger = kill.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(Plan.EndX - LevelPlanner.StartX + 40f, 4f);
            kill.AddComponent<KillZone>();
        }
    }
}
