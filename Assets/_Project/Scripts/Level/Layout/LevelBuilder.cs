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

        [Header("Prefabs")]
        [SerializeField] private GroundSegment _groundPrefab;
        [SerializeField] private QuestionZone _zonePrefab;
        [SerializeField] private Obstacle _spikePrefab;
        [SerializeField] private Checkpoint _checkpointPrefab;
        [SerializeField] private FinishFlag _finishPrefab;

        public LevelConfig Level { get; private set; }
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

            float airTime = _player.AirTimeToLandOn(_zonePrefab.PlatformTop);
            var settings = LevelPlanSettings.From(config, airTime + _zonePrefab.PlatformSpanSeconds);
            Plan = LevelPlanner.Create(settings);
            var root = transform;

            foreach (var (start, end) in Plan.Ground)
                Instantiate(_groundPrefab, root).SetRect(start, -GroundDepth, end, 0f);
            Instantiate(_groundPrefab, root).SetRect(Plan.EndX, -GroundDepth, Plan.EndX + 2f, WallHeight);

            foreach (float x in Plan.Spikes)
                Instantiate(_spikePrefab, new Vector3(x, 0f, 0f), Quaternion.identity, root);

            foreach (var zonePlan in Plan.Zones)
            {
                var zone = Instantiate(_zonePrefab, new Vector3(zonePlan.X, 0f, 0f), Quaternion.identity, root);
                zone.Configure(config.RunSpeed, zonePlan.Timed ? config.TimeLimit : 0f, zonePlan.Lead, airTime);
                zone.SetDirector(_director);
            }

            foreach (float x in Plan.Checkpoints)
                Instantiate(_checkpointPrefab, new Vector3(x, 1f, 0f), Quaternion.identity, root);

            Instantiate(_finishPrefab, new Vector3(Plan.FinishX, 0f, 0f), Quaternion.identity, root);

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
