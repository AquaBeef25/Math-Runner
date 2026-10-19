using MathRunner.UI;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class LevelButtonTests
    {
        [Test]
        public void ColorFollowsWorldAndLock()
        {
            Assert.That(LevelButton.ColorFor(false, true), Is.EqualTo(UiButtonColor.Green));
            Assert.That(LevelButton.ColorFor(true, true), Is.EqualTo(UiButtonColor.Yellow));
            Assert.That(LevelButton.ColorFor(true, false), Is.EqualTo(UiButtonColor.Grey));
            Assert.That(LevelButton.ColorFor(false, false), Is.EqualTo(UiButtonColor.Grey));
        }
    }
}
