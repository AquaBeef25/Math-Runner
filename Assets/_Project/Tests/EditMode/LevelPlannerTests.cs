using System.Linq;
using MathRunner.Level;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class LevelPlannerTests
    {
        private static LevelPlanSettings Settings(int count = 8, float speed = 6f, float ratio = 0.5f, float limit = 4f,
            int hazards = 2, float gapChance = 0.5f, int seed = 1) => new LevelPlanSettings
        {
            QuestionCount = count,
            RunSpeed = speed,
            TimedRatio = ratio,
            TimeLimit = limit,
            UntimedLeadSeconds = 6f,
            ZoneSpanSeconds = 2f,
            HazardsBetweenQuestions = hazards,
            GapChance = gapChance,
            Seed = seed,
            IntroSeconds = 2f,
            HazardSpacingSeconds = 1.6f,
            GapSeconds = 0.4f,
            AfterZoneSeconds = 3f,
            OutroSeconds = 2f,
        };

        private static bool OnGround(LevelPlan plan, float from, float to) =>
            plan.Ground.Any(g => g.Start <= from && to <= g.End);

        [Test]
        public void PlansOneZonePerQuestion([Values(1, 8, 12)] int count)
        {
            Assert.That(LevelPlanner.Create(Settings(count)).Zones.Count, Is.EqualTo(count));
        }

        [Test]
        public void EveryZoneSitsOnSolidGroundWithNoSpikes([Values(5f, 6f, 8f)] float speed, [Range(1, 20)] int seed)
        {
            var s = Settings(speed: speed, seed: seed);
            var plan = LevelPlanner.Create(s);
            for (int i = 0; i < plan.Zones.Count; i++)
            {
                float start = plan.Zones[i].X - 1f, end = plan.Zones[i].X + plan.ZoneLength(i, s) + 1f;
                Assert.That(OnGround(plan, start, end), Is.True, $"Zone {i} at {plan.Zones[i].X} crosses a gap");
                Assert.That(plan.Spikes.Any(x => x > start && x < end), Is.False, $"Zone {i} contains a spike");
            }
        }

        [Test]
        public void SpikesStandOnGroundAndGapsAreShortEnoughToJump([Values(5f, 8f)] float speed, [Range(1, 20)] int seed)
        {
            var s = Settings(speed: speed, seed: seed, hazards: 3);
            var plan = LevelPlanner.Create(s);
            foreach (float x in plan.Spikes) Assert.That(OnGround(plan, x - 0.5f, x + 0.5f), Is.True, $"Spike at {x} is over a gap");
            for (int i = 0; i + 1 < plan.Ground.Count; i++)
            {
                float gap = plan.Ground[i + 1].Start - plan.Ground[i].End;
                Assert.That(gap, Is.EqualTo(s.GapSeconds * speed).Within(0.001f));
                // A held jump covers about 0.76 s of running, so 0.4 s gaps leave a wide timing window.
                Assert.That(gap, Is.LessThan(0.6f * speed));
            }
        }

        [Test]
        public void HazardsAreAtLeastOneSpacingApart([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed, hazards: 3);
            var plan = LevelPlanner.Create(s);
            var hazards = plan.Spikes.Concat(plan.Ground.Skip(1).Select(g => g.Start - s.GapSeconds * s.RunSpeed)).OrderBy(x => x).ToArray();
            for (int i = 0; i + 1 < hazards.Length; i++)
                Assert.That(hazards[i + 1] - hazards[i], Is.GreaterThanOrEqualTo(s.HazardSpacingSeconds * s.RunSpeed - 0.001f));
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
            Assert.That(b.Zones, Is.EqualTo(a.Zones));
        }

        [Test]
        public void FinishAndCheckpointsAreOnGround([Range(1, 10)] int seed)
        {
            var s = Settings(seed: seed);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.FinishX, Is.GreaterThan(plan.Zones.Last().X + plan.ZoneLength(plan.Zones.Count - 1, s)));
            Assert.That(OnGround(plan, plan.FinishX - 1f, plan.EndX), Is.True);
            Assert.That(plan.Checkpoints.Count, Is.EqualTo(s.QuestionCount));
            foreach (float c in plan.Checkpoints) Assert.That(OnGround(plan, c - 1f, c + 1f), Is.True, $"Checkpoint at {c} is over a gap");
        }
    }
}
