using MathRunner.Level;
using NUnit.Framework;

namespace MathRunner.Tests.EditMode
{
    public class LevelThemeTests
    {
        // For now the game has five levels: the Meadow (1-3) and the Desert (4-5).
        [Test]
        public void FiveLevelsMeadowThenDesert()
        {
            var levels = LevelCatalog.Load().Levels;
            Assert.That(levels.Count, Is.EqualTo(5));
            for (int i = 0; i < levels.Count; i++)
            {
                Assert.That(levels[i].LevelNumber, Is.EqualTo(i + 1));
                Assert.That(levels[i].Background.name, Is.EqualTo(i < 3 ? "Hills" : "Desert"), $"Level {i + 1}");
            }
        }
    }
}
