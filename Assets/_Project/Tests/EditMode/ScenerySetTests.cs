using MathRunner.Level;
using NUnit.Framework;
using UnityEditor;

namespace MathRunner.Tests
{
    // Flies and frogs are encounter enemies, so the harmless scenery must never show them.
    public class ScenerySetTests
    {
        [Test]
        public void ScenerySetsHaveNoFliesOrFrogs([Values("MeadowLife", "DesertLife")] string set)
        {
            var life = AssetDatabase.LoadAssetAtPath<LifeSet>($"Assets/_Project/ScriptableObjects/Worlds/{set}.asset");
            Assert.That(life, Is.Not.Null);
            Assert.That(life.Weights[(int)LifeKind.Flyer], Is.EqualTo(0f), "no harmless flies");
            Assert.That(life.Weights[(int)LifeKind.Hopper], Is.EqualTo(0f), "no harmless frogs");
            Assert.That(life.Weights[(int)LifeKind.Flag], Is.EqualTo(0f), "no random flags");
            Assert.That(life.Weights[(int)LifeKind.Walker], Is.GreaterThan(0f), "critters stay");
        }
    }
}
