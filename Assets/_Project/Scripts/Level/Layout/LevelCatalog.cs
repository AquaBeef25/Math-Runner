using System.Collections.Generic;
using UnityEngine;

namespace MathRunner.Level
{
    // Ordered list of playable levels. Lives in a Resources folder so menus and tests can load it without a scene reference.
    [CreateAssetMenu(menuName = "MathRunner/Level Catalog", fileName = "LevelCatalog")]
    public class LevelCatalog : ScriptableObject
    {
        public const string ResourcePath = "LevelCatalog";

        [SerializeField] private List<LevelConfig> _levels = new List<LevelConfig>();

        public IReadOnlyList<LevelConfig> Levels => _levels;

        public static LevelCatalog Load() => Resources.Load<LevelCatalog>(ResourcePath);

        public LevelConfig Next(LevelConfig current)
        {
            int i = _levels.IndexOf(current);
            return i >= 0 && i + 1 < _levels.Count ? _levels[i + 1] : null;
        }
    }

    // The level chosen in the menu; read by the LevelBuilder when the Game scene loads.
    public static class LevelSelection
    {
        public static LevelConfig Current { get; set; }
    }
}
