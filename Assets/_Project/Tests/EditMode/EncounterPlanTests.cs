using System.Linq;
using MathRunner.Level;
using NUnit.Framework;

namespace MathRunner.Tests
{
    // Which enemy each encounter meets: random per level seed, reproducible, never two flies in a row.
    public class EncounterPlanTests
    {
        private static readonly float[] Equal = { 1f, 1f, 1f, 1f };

        private static LevelPlanSettings Settings(int seed = 1, float[] enemies = null, int questions = 8) => new LevelPlanSettings
        {
            QuestionCount = questions,
            RunSpeed = 6f,
            TimedRatio = 0.5f,
            TimeLimit = 4f,
            UntimedLeadSeconds = 6f,
            ZoneSpanSeconds = 2f,
            FocusScale = 0.5f,
            HazardsBetweenQuestions = 2,
            GapChance = 0.3f,
            Seed = seed,
            IntroSeconds = 2f,
            HazardSpacingSeconds = 1.6f,
            GapSeconds = 0.4f,
            AfterZoneSeconds = 3f,
            OutroSeconds = 2f,
            JumpHeight = 3.2f,
            Gravity = -9.81f,
            GravityScale = 4f,
            FallMultiplier = 1.4f,
            MaxStepTiles = 2,
            RouteSplits = 2,
            PlatformChance = 0.5f,
            RampChance = 0.4f,
            CoinChance = 0.5f,
            LifeDensity = 0.6f,
            LifeWeights = new[] { 1f, 0f, 0f, 1f, 1f, 0f },
            LifeVariants = new[] { 1, 1, 1, 1, 1, 1 },
            EnemyWeights = enemies,
        };

        [Test]
        public void EveryEncounterHasAnEnemy([Range(1, 5)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed, Equal));
            Assert.That(plan.ZoneEnemies.Count, Is.EqualTo(plan.Zones.Count));
        }

        [Test]
        public void SameSeedSameEnemies([Range(1, 5)] int seed)
        {
            var s = Settings(seed, Equal);
            Assert.That(LevelPlanner.Create(s).ZoneEnemies, Is.EqualTo(LevelPlanner.Create(s).ZoneEnemies));
        }

        [Test]
        public void EnemiesNeverChangeTheRestOfThePlan([Range(1, 5)] int seed)
        {
            var with = LevelPlanner.Create(Settings(seed, Equal));
            var without = LevelPlanner.Create(Settings(seed, null));
            Assert.That(with.Ground, Is.EqualTo(without.Ground));
            Assert.That(with.Jumps, Is.EqualTo(without.Jumps));
            Assert.That(with.Coins, Is.EqualTo(without.Coins));
            Assert.That(with.Life, Is.EqualTo(without.Life));
            Assert.That(with.Zones, Is.EqualTo(without.Zones));
        }

        [Test]
        public void NeverTwoFliesInARow([Range(1, 30)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed, Equal, questions: 12));
            for (int i = 1; i < plan.ZoneEnemies.Count; i++)
                Assert.That(plan.ZoneEnemies[i].Kind == EnemyKind.Fly && plan.ZoneEnemies[i - 1].Kind == EnemyKind.Fly, Is.False, $"flies at {i - 1} and {i}");
        }

        [Test]
        public void OnlyFliesAlternateWithAliens()
        {
            var plan = LevelPlanner.Create(Settings(1, new[] { 0f, 0f, 0f, 1f }));
            for (int i = 0; i < plan.ZoneEnemies.Count; i++)
                Assert.That(plan.ZoneEnemies[i].Kind, Is.EqualTo(i % 2 == 0 ? EnemyKind.Fly : EnemyKind.Alien));
        }

        [Test]
        public void AlienBeeAndFlyAppearButNeverTheFrog()
        {
            // The frog was retired on 2026-10-19 (the user felt it didn't fit): even a weight for it never picks it.
            var kinds = Enumerable.Range(1, 10).SelectMany(seed => LevelPlanner.Create(Settings(seed, Equal)).ZoneEnemies.Select(e => e.Kind)).Distinct().ToList();
            Assert.That(kinds, Is.EquivalentTo(new[] { EnemyKind.Alien, EnemyKind.Bee, EnemyKind.Fly }));
        }

        [Test]
        public void FrogOnlyWeightsFallBackToTheAlien()
        {
            var plan = LevelPlanner.Create(Settings(1, new[] { 0f, 0f, 1f, 0f }));
            Assert.That(plan.ZoneEnemies.All(e => e.Kind == EnemyKind.Alien), Is.True);
        }

        // While answering, the runner must keep moving without a jump: no step up or pillar inside an encounter
        // stretch (ramps are fine), and so no planned jump there either.
        [Test]
        public void EncounterStretchesNeedNoJump([Range(1, 20)] int seed)
        {
            var s = Settings(seed, Equal);
            var plan = LevelPlanner.Create(s);
            for (int i = 0; i < plan.Zones.Count; i++)
            {
                float start = plan.Zones[i].X, end = start + plan.ZoneLength(i, s);
                Assert.That(plan.Jumps.Any(j => j.X >= start && j.X <= end), Is.False, $"a jump is needed inside encounter {i}");
                for (int g = 0; g + 1 < plan.Ground.Count; g++)
                {
                    var a = plan.Ground[g];
                    var b = plan.Ground[g + 1];
                    bool wall = b.Top > a.Top && System.Math.Abs(b.Start - a.End) < 1e-3f && b.End - b.Start > 1e-3f;
                    if (wall && a.End >= start && a.End <= end) Assert.Fail($"a step up at {a.End} blocks the runner in encounter {i}");
                }
            }
        }

        [Test]
        public void AliensAppearAboveLeftAndAboveRight()
        {
            var spots = Enumerable.Range(1, 10).SelectMany(seed => LevelPlanner.Create(Settings(seed, new[] { 1f, 0f, 0f, 0f })).ZoneEnemies).ToList();
            Assert.That(spots.Any(e => e.SpotX < 0f), Is.True, "some aliens above-left");
            Assert.That(spots.Any(e => e.SpotX > 0f), Is.True, "some aliens above-right");
            foreach (var e in spots) Assert.That(System.Math.Abs(e.SpotX), Is.InRange(3f, 7f));
        }

        [Test]
        public void AlienSpotsStayInTheVisibleSky([Range(1, 10)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed, new[] { 1f, 0f, 0f, 0f }));
            Assert.That(plan.ZoneEnemies.Select(e => e.SpotX).Distinct().Count(), Is.GreaterThan(1), "aliens appear at different spots");
            foreach (var e in plan.ZoneEnemies)
            {
                Assert.That(e.Kind, Is.EqualTo(EnemyKind.Alien));
                Assert.That(System.Math.Abs(e.SpotX), Is.InRange(3f, 7f));
                Assert.That(e.SpotY, Is.InRange(2.5f, 4.0f), "below the question panel even at the top of its bob");
            }
        }

        [Test]
        public void NoWeightsMeansAllAliens()
        {
            var plan = LevelPlanner.Create(Settings(3, null));
            Assert.That(plan.ZoneEnemies.Count, Is.EqualTo(plan.Zones.Count));
            Assert.That(plan.ZoneEnemies.All(e => e.Kind == EnemyKind.Alien), Is.True);
        }
    }
}
