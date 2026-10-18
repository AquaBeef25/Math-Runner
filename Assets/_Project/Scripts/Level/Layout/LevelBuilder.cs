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
        [SerializeField] private RampPiece _rampPrefab;
        [SerializeField] private WaterPool _waterPrefab;
        [SerializeField] private Walker _walkerPrefab;
        [SerializeField] private Saw _sawPrefab;
        [SerializeField] private Bee _beePrefab;
        [SerializeField] private Coin _coinPrefab;
        [SerializeField, Min(0f)] private float _beeHz = 0.8f;
        [SerializeField] private SpriteRenderer _decorationPrefab;
        [Header("Backdrop")]
        [SerializeField] private int _backdropOrder = -5;
        [Tooltip("How much of the camera's movement backdrop props copy, so they read as slightly farther away.")]
        [SerializeField] private float _backdropFollow = 0.12f;
        [SerializeField] private Color _backdropTint = new Color(0.9f, 0.95f, 1f, 0.9f);
        [SerializeField, Min(0f)] private float _walkerSpeed = 1f;
        [SerializeField] private Checkpoint _checkpointPrefab;
        [SerializeField] private FinishFlag _finishPrefab;

        public LevelConfig Level { get; private set; }

        // Editor setup: ScriptableObject scene fields don't save reliably through SerializedProperty here.
        public void SetEncounterSettings(EncounterSettings settings) => _encounter = settings;
        public LevelPlan Plan { get; private set; }

        private GroundSegment Ground(Transform root, LevelConfig config)
        {
            var ground = Instantiate(_groundPrefab, root);
            if (config.Terrain != null) ground.SetTerrain(config.Terrain);
            return ground;
        }

        // Top of the ground segment touching x from the left (or right), or null when x is a gap edge.
        private float? NeighbourTop(float x, bool after)
        {
            foreach (var g in Plan.Ground)
                if (g.End - g.Start > 1e-3f && System.Math.Abs((after ? g.Start : g.End) - x) < 1e-3f) return g.Top;
            return null;
        }

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

            float endTop = Plan.GroundTopAt(Plan.EndX);
            float wallTop = endTop + WallHeight;
            foreach (var (start, end, top) in Plan.Ground)
            {
                // Outline each side only where it's exposed; where a ramp continues the ground there's no outline at all.
                float leftEdge = Plan.Ramps.Exists(r => System.Math.Abs(r.End - start) < 1e-3f) ? float.PositiveInfinity
                    : GroundSegment.EdgeFrom(top, NeighbourTop(start, false));
                float rightEdge = Plan.Ramps.Exists(r => System.Math.Abs(r.Start - end) < 1e-3f) ? float.PositiveInfinity
                    : System.Math.Abs(end - Plan.EndX) < 1e-3f ? GroundSegment.EdgeFrom(top, wallTop)
                    : GroundSegment.EdgeFrom(top, NeighbourTop(end, true));
                Ground(root, config).SetRect(start, -GroundDepth, end, top, leftEdge, rightEdge);
            }
            Ground(root, config).SetRect(Plan.EndX, -GroundDepth, Plan.EndX + 2f, wallTop,
                GroundSegment.EdgeFrom(wallTop, endTop), float.NegativeInfinity);

            foreach (var (x, y) in Plan.Spikes)
                Instantiate(_spikePrefab, new Vector3(x, y, 0f), Quaternion.identity, root);

            foreach (var (left, right, surface) in Plan.Water)
                Instantiate(_waterPrefab, root).SetRect(left, right, surface, -GroundDepth);

            foreach (var (start, end, from, to) in Plan.Ramps)
            {
                var ramp = Instantiate(_rampPrefab, root);
                var t = config.Terrain;
                if (t != null) ramp.SetSprites(t.RampHigh, t.RampLow, t.RampUnder, t.Fill, t.Top);
                ramp.SetRamp(start, end, from, to, -GroundDepth);
            }

            for (int i = 0; i < Plan.Platforms.Count; i++)
            {
                var (left, right, top) = Plan.Platforms[i];
                var platform = Instantiate(_platformPrefab, root);
                if (config.Terrain != null) platform.SetTerrain(config.Terrain);
                platform.SetRect(left, right, top);
                platform.SetStyle(Plan.PlatformStyles[i], Plan.GroundTopAt((left + right) * 0.5f));
            }

            foreach (var (left, right, y) in Plan.Walkers)
                Instantiate(_walkerPrefab, new Vector3(left, y, 0f), Quaternion.identity, root).Configure(left, right, _walkerSpeed);

            foreach (var (x, y) in Plan.Saws)
                Instantiate(_sawPrefab, new Vector3(x, y, 0f), Quaternion.identity, root);

            if (_beePrefab != null)
                foreach (var (x, baseY, amplitude) in Plan.Bees)
                    Instantiate(_beePrefab, root).Configure(x, baseY, amplitude, _beeHz);

            if (_coinPrefab != null)
                foreach (var (x, y) in Plan.Coins)
                    Instantiate(_coinPrefab, new Vector3(x, y, 0f), Quaternion.identity, root);

            if (config.Decorations != null && _decorationPrefab != null)
                foreach (var (x, y, kind) in Plan.Decorations)
                    Instantiate(_decorationPrefab, new Vector3(x, y, 0f), Quaternion.identity, root).sprite = config.Decorations.Sprite(kind);

            if (config.Decorations != null && Plan.Backdrop.Count > 0)
            {
                var backdrop = new GameObject("Backdrop").transform;
                backdrop.SetParent(root, false);
                foreach (var (x, y, kind) in Plan.Backdrop)
                {
                    var prop = new GameObject("Prop");
                    prop.transform.SetParent(backdrop, false);
                    prop.transform.position = new Vector3(x, y, 0f);
                    var sr = prop.AddComponent<SpriteRenderer>();
                    sr.sprite = config.Decorations.BackdropSprite(kind);
                    sr.sortingOrder = _backdropOrder;
                    sr.color = _backdropTint;
                    // Anchored so the prop stands exactly at x when the camera is over it, drifting a little either side.
                    prop.AddComponent<ParallaxLayer>().Configure(new Vector2(_backdropFollow, 0f), new Vector2(x * (1f - _backdropFollow), y), 0f);
                }
            }

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
