using System.Linq;
using MathRunner.Level;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class LevelPlannerTests
    {
        private static LevelPlanSettings Settings(int count = 8, float speed = 6f, float ratio = 0.5f, float limit = 4f,
            int hazards = 2, float gapChance = 0.3f, int seed = 1, int maxStep = 2, int splits = 2, float platforms = 0.5f) => new LevelPlanSettings
        {
            QuestionCount = count,
            RunSpeed = speed,
            TimedRatio = ratio,
            TimeLimit = limit,
            UntimedLeadSeconds = 6f,
            ZoneSpanSeconds = 2f,
            FocusScale = 0.5f,
            HazardsBetweenQuestions = hazards,
            GapChance = gapChance,
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
            MaxStepTiles = maxStep,
            RouteSplits = splits,
            PlatformChance = platforms,
        };

        private static bool OnGround(LevelPlan plan, float from, float to) =>
            plan.Ground.Any(g => g.Start <= from && to <= g.End);

        private static int HeightChanges(LevelPlan plan) =>
            plan.Ground.Zip(plan.Ground.Skip(1), (a, b) => a.Top != b.Top ? 1 : 0).Sum();

        [Test]
        public void PlansOneZonePerQuestion([Values(1, 8, 12)] int count)
        {
            Assert.That(LevelPlanner.Create(Settings(count)).Zones.Count, Is.EqualTo(count));
        }

        [Test]
        public void EncounterStretchUsesSlowedFocusTime()
        {
            var s = Settings(ratio: 1f, limit: 4f);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.ZoneLength(0, s), Is.EqualTo((4f * 0.5f + 2f) * 6f).Within(1e-4f));
        }

        [Test]
        public void EncounterStretchesHaveNoGaps([Values(5f, 6f, 8f)] float speed, [Range(1, 20)] int seed)
        {
            var s = Settings(speed: speed, seed: seed, gapChance: 0.6f);
            var plan = LevelPlanner.Create(s);
            for (int i = 0; i < plan.Zones.Count; i++)
            {
                float start = plan.Zones[i].X - 1f, end = plan.Zones[i].X + plan.ZoneLength(i, s) + 1f;
                for (float x = start; x <= end; x += 0.25f)
                    Assert.That(float.IsNaN(plan.GroundTopAt(x)), Is.False, $"Zone {i} has a gap at {x}");
            }
        }

        [Test]
        public void EveryStepIsReachableAndHeightsStayInRange([Values(5f, 6f, 8f)] float speed, [Range(1, 20)] int seed)
        {
            var s = Settings(speed: speed, seed: seed);
            var plan = LevelPlanner.Create(s);
            foreach (var g in plan.Ground) Assert.That(g.Top, Is.InRange(0f, 3f));
            for (int i = 0; i + 1 < plan.Ground.Count; i++)
            {
                var a = plan.Ground[i];
                var b = plan.Ground[i + 1];
                float rise = b.Top - a.Top;
                float gap = b.Start - a.End;
                Assert.That(rise, Is.LessThanOrEqualTo(s.MaxStepTiles), $"Step at {b.Start} rises {rise}");
                Assert.That(JumpReach.CanReach(s, gap, rise), Is.True, $"Can't reach ground at {b.Start} (gap {gap}, rise {rise})");
            }
        }

        [Test]
        public void EveryPlatformIsReachableFromBelowOrTheOneBefore([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed, platforms: 1f, splits: 3);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.Platforms, Is.Not.Empty);
            foreach (var p in plan.Platforms)
            {
                float groundBelow = plan.GroundTopAt(p.Left);
                bool fromGround = !float.IsNaN(groundBelow) && JumpReach.CanReach(s, 0.5f, p.Top - groundBelow);
                bool fromPrevious = plan.Platforms.Any(q => q.Right < p.Left && JumpReach.CanReach(s, p.Left - q.Right, p.Top - q.Top));
                Assert.That(fromGround || fromPrevious, Is.True, $"Platform at {p.Left} (top {p.Top}) is unreachable");
                Assert.That(p.Right - p.Left, Is.InRange(2f, 4f));
            }
        }

        [Test]
        public void BothRoutesOfASplitRejoinSolidGround([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed, splits: 3);
            var plan = LevelPlanner.Create(s);
            foreach (var p in plan.Platforms)
            {
                Assert.That(OnGround(plan, p.Left, p.Right), Is.True, $"Ground missing under platform at {p.Left}");
                Assert.That(float.IsNaN(plan.GroundTopAt(p.Right + 2f)), Is.False, $"High route ending at {p.Right} drops into a gap");
            }
        }

        [Test]
        public void EachLevelHasItsRouteSplits()
        {
            var s = Settings(splits: 2, platforms: 0f);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.Splits, Is.EqualTo(2));
        }

        [Test]
        public void SpikesStandOnTheGroundTop([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, hazards: 3));
            foreach (var sp in plan.Spikes)
            {
                Assert.That(OnGround(plan, sp.X - 0.5f, sp.X + 0.5f), Is.True, $"Spike at {sp.X} is over a gap");
                Assert.That(sp.Y, Is.EqualTo(plan.GroundTopAt(sp.X)).Within(1e-4f), $"Spike at {sp.X} floats or is buried");
            }
        }

        [Test]
        public void EveryHazardHasATakeOffBeforeIt([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed, hazards: 3);
            var plan = LevelPlanner.Create(s);
            foreach (var sp in plan.Spikes)
                Assert.That(plan.Jumps.Any(j => j.X < sp.X && sp.X - j.X < 0.6f * s.RunSpeed), Is.True, $"No jump planned before spike at {sp.X}");
            for (int i = 0; i + 1 < plan.Ground.Count; i++)
            {
                bool gapOrRise = plan.Ground[i + 1].Start - plan.Ground[i].End > 0.01f || plan.Ground[i + 1].Top > plan.Ground[i].Top;
                if (!gapOrRise) continue;
                float edge = plan.Ground[i].End;
                Assert.That(plan.Jumps.Any(j => j.X < edge && edge - j.X < 0.6f * s.RunSpeed), Is.True, $"No jump planned before the edge at {edge}");
            }
        }

        [Test]
        public void CheckpointsSitOnTheGroundTop([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.Checkpoints.Count, Is.EqualTo(s.QuestionCount));
            foreach (var c in plan.Checkpoints)
            {
                Assert.That(OnGround(plan, c.X - 1f, c.X + 1f), Is.True, $"Checkpoint at {c.X} is over a gap");
                Assert.That(c.Y, Is.EqualTo(plan.GroundTopAt(c.X) + 1f).Within(1e-4f), $"Checkpoint at {c.X} isn't 1 unit above the ground");
            }
        }

        [Test]
        public void HarderLevelsHaveMoreVariety()
        {
            int easy = 0, hard = 0;
            for (int seed = 1; seed <= 10; seed++)
            {
                easy += HeightChanges(LevelPlanner.Create(Settings(seed: seed, maxStep: 1, platforms: 0.2f, splits: 1)));
                hard += HeightChanges(LevelPlanner.Create(Settings(seed: seed, maxStep: 2, platforms: 0.5f, splits: 3)));
            }
            Assert.That(hard, Is.GreaterThan(easy));
        }

        [Test]
        public void FlatSettingsGiveFlatGround()
        {
            var plan = LevelPlanner.Create(Settings(maxStep: 0, splits: 0, platforms: 0f));
            Assert.That(plan.Ground.All(g => g.Top == 0f), Is.True);
            Assert.That(plan.Platforms, Is.Empty);
        }

        [Test]
        public void TimedQuestionsFollowTheRatio()
        {
            Assert.That(LevelPlanner.Create(Settings(ratio: 0f)).Zones.Count(z => z.Timed), Is.Zero);
            Assert.That(LevelPlanner.Create(Settings(ratio: 1f)).Zones.All(z => z.Timed), Is.True);
            var half = LevelPlanner.Create(Settings(count: 8, ratio: 0.5f));
            Assert.That(half.Zones.Count(z => z.Timed), Is.EqualTo(4));
            Assert.That(half.Zones[0].Timed, Is.False, "The first question should be untimed when not every question is timed");
            Assert.That(LevelPlanner.Create(Settings(ratio: 1f, limit: 0f)).Zones.Count(z => z.Timed), Is.Zero, "No time limit means nothing can be timed");
        }

        [Test]
        public void UntimedRunUpIsNeverShorterThanTheTimeLimit()
        {
            var plan = LevelPlanner.Create(Settings(ratio: 0.5f, limit: 10f));
            foreach (var z in plan.Zones) Assert.That(z.Lead, Is.GreaterThanOrEqualTo(10f));
        }

        [Test]
        public void SameSeedGivesTheSameLayout()
        {
            var a = LevelPlanner.Create(Settings(seed: 7));
            var b = LevelPlanner.Create(Settings(seed: 7));
            Assert.That(b.Spikes, Is.EqualTo(a.Spikes));
            Assert.That(b.Ground, Is.EqualTo(a.Ground));
            Assert.That(b.Platforms, Is.EqualTo(a.Platforms));
            Assert.That(b.Zones, Is.EqualTo(a.Zones));
            Assert.That(b.Jumps, Is.EqualTo(a.Jumps));
        }

        [Test]
        public void FinishIsOnGroundAfterTheLastEncounter([Range(1, 10)] int seed)
        {
            var s = Settings(seed: seed);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.FinishX, Is.GreaterThan(plan.Zones.Last().X + plan.ZoneLength(plan.Zones.Count - 1, s)));
            Assert.That(OnGround(plan, plan.FinishX - 1f, plan.EndX), Is.True);
        }
    }
}
