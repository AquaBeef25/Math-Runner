using System.Linq;
using MathRunner.Level;
using MathRunner.Player;
using NUnit.Framework;

namespace MathRunner.Tests
{
    // Fork in the path: a question zone where the road splits into a high ledge (full jump), a middle ledge (short hop)
    // and a low road dipping into a hollow (no jump). The jump the runner makes is the answer.
    public class ForkPlanTests
    {
        private const int High = 0, Middle = 1, Low = 2;
        private const float LedgeThickness = 0.4f, BodyHeight = 1f, HeadRoom = 0.25f;

        private static LevelPlanSettings Settings(int seed = 1, float forkShare = 1f, float speed = 6f, int questions = 6) => new LevelPlanSettings
        {
            QuestionCount = questions,
            RunSpeed = speed,
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
            RouteSplits = 1,
            PlatformChance = 0.5f,
            RampChance = 0.4f,
            CoinChance = 0.5f,
            DecorationDensity = 1f,
            DecorationWeights = new[] { 1f, 1f },
            LifeDensity = 0.6f,
            LifeWeights = new[] { 1f, 0f, 1f, 0f, 0f, 0f },
            LifeVariants = new[] { 1, 1, 1, 1, 1, 1 },
            BackdropWeights = new[] { 1f, 1f },
            EnemyWeights = new[] { 1f, 1f, 0f, 1f },
            ForkShare = forkShare,
        };

        [Test]
        public void NoForksWhenTheShareIsZero([Range(1, 10)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed, 0f));
            Assert.That(plan.Forks, Is.Empty);
            Assert.That(plan.ZoneKinds.All(k => k == ZoneKind.Encounter), Is.True);
            Assert.That(plan.ZoneKinds.Count, Is.EqualTo(plan.Zones.Count));
        }

        [Test]
        public void EveryQuestionIsAForkWhenTheShareIsOne([Range(1, 10)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed));
            Assert.That(plan.ZoneKinds.All(k => k == ZoneKind.Fork), Is.True);
            Assert.That(plan.Forks.Count, Is.EqualTo(plan.Zones.Count));
            for (int i = 0; i < plan.Forks.Count; i++) Assert.That(plan.Forks[i].ZoneIndex, Is.EqualTo(i));
        }

        [Test]
        public void AboutHalfAreForksAtHalfShare()
        {
            int forks = 0, zones = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                var plan = LevelPlanner.Create(Settings(seed, 0.5f, questions: 8));
                forks += plan.Forks.Count;
                zones += plan.Zones.Count;
            }
            Assert.That(forks / (float)zones, Is.InRange(0.35f, 0.65f));
        }

        [Test]
        public void SameSeedSameForks()
        {
            var a = LevelPlanner.Create(Settings(3, 0.5f));
            var b = LevelPlanner.Create(Settings(3, 0.5f));
            Assert.That(a.ZoneKinds, Is.EqualTo(b.ZoneKinds));
            Assert.That(a.Forks.Select(f => f.SplitX), Is.EqualTo(b.Forks.Select(f => f.SplitX)));
        }

        // The hop (shortest tap and the planner's 0.1 s) lands on the middle ledge; even a 0.2 s hold never reaches the high one.
        [Test]
        public void HopLandsOnTheMiddleLedgeOnly([Range(1, 8)] int seed, [Values(5f, 6f, 8f)] float speed)
        {
            var s = Settings(seed, speed: speed);
            foreach (var f in LevelPlanner.Create(s).Forks)
            {
                var mid = f.Lanes[Middle];
                var high = f.Lanes[High];
                Assert.That(mid.HoldSeconds, Is.GreaterThan(0f).And.LessThan(0.2f));
                foreach (float hold in new[] { 0f, mid.HoldSeconds, 0.2f })
                {
                    float rise = mid.Top - f.BaseTop;
                    float air = JumpMath.CutJumpAirTime(s.JumpHeight, s.Gravity, s.GravityScale, s.FallMultiplier, hold, 0.08f, 0.5f, rise);
                    float landX = mid.TakeoffX + air * speed;
                    Assert.That(landX, Is.InRange(mid.Left + 0.3f, mid.Right - 0.3f), $"hold {hold}: lands at {landX} on [{mid.Left}, {mid.Right}]");
                    Assert.That(JumpMath.CutJumpPeak(s.JumpHeight, s.Gravity, s.GravityScale, hold, 0.08f, 0.5f), Is.LessThan(high.Top - f.BaseTop - 0.05f), $"hold {hold} must not reach the high ledge");
                }
                Assert.That(JumpReach.CanHopReach(s, 1f, mid.Top - f.BaseTop), Is.True);
            }
        }

        [Test]
        public void FullJumpLandsOnTheHighLedge([Range(1, 8)] int seed, [Values(5f, 6f, 8f)] float speed)
        {
            var s = Settings(seed, speed: speed);
            foreach (var f in LevelPlanner.Create(s).Forks)
            {
                var high = f.Lanes[High];
                float rise = high.Top - f.BaseTop;
                Assert.That(rise, Is.LessThanOrEqualTo(s.JumpHeight * 0.85f), "inside the full jump's safety margin");
                float landX = high.TakeoffX + JumpReach.AirTime(s, rise) * speed;
                Assert.That(landX, Is.InRange(high.Left + 0.3f, high.Right - 0.3f), $"lands at {landX} on [{high.Left}, {high.Right}]");
                Assert.That(high.HoldSeconds, Is.GreaterThanOrEqualTo(0.4f));
            }
        }

        // Nobody bumps their head: the low road runs under the middle ledge, the middle ledge under the high one.
        [Test]
        public void EveryLaneHasHeadRoom([Range(1, 8)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed));
            foreach (var f in plan.Forks)
            {
                var mid = f.Lanes[Middle];
                var high = f.Lanes[High];
                for (float x = mid.Left; x <= mid.Right; x += 0.1f)
                    Assert.That(plan.GroundTopAt(x) + BodyHeight, Is.LessThanOrEqualTo(mid.Top - LedgeThickness - HeadRoom), $"low road under the middle ledge at {x}");
                Assert.That(mid.Top + BodyHeight, Is.LessThanOrEqualTo(high.Top - LedgeThickness - HeadRoom + 1e-4f));
                for (float x = high.Left; x <= high.Right; x += 0.1f)
                    Assert.That(plan.GroundTopAt(x) + BodyHeight, Is.LessThanOrEqualTo(high.Top - LedgeThickness - HeadRoom), $"low road under the high ledge at {x}");
            }
        }

        [Test]
        public void LowRoadDipsIntoAHollow([Range(1, 8)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed));
            foreach (var f in plan.Forks)
            {
                var low = f.Lanes[Low];
                Assert.That(low.Top, Is.EqualTo(f.BaseTop - 1f));
                Assert.That(float.IsNaN(low.TakeoffX), Is.True, "no jump on the low road");
                for (float x = low.Left; x <= low.Right; x += 0.25f)
                    Assert.That(plan.GroundTopAt(x), Is.EqualTo(low.Top).Within(1e-4f), $"flat hollow floor at {x}");
                Assert.That(plan.GroundTopAt(f.Lanes[Middle].TakeoffX), Is.EqualTo(f.BaseTop), "take-off on the ground before the dip");
            }
        }

        // The ledges end over the flat hollow floor (not over a ramp), and the road climbs back to the fork's height after.
        [Test]
        public void LanesRejoinOnFlatGround([Range(1, 8)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed));
            foreach (var f in plan.Forks)
            {
                foreach (int lane in new[] { High, Middle })
                {
                    float end = f.Lanes[lane].Right;
                    Assert.That(plan.SurfaceSlopeAt(end + 0.5f), Is.EqualTo(0f), "flat where the runner drops off");
                    Assert.That(plan.GroundTopAt(end + 0.5f), Is.EqualTo(f.Lanes[Low].Top).Within(1e-4f));
                }
                Assert.That(plan.GroundTopAt(f.EndX), Is.EqualTo(f.BaseTop).Within(1e-4f), "back up at the end");
            }
        }

        [Test]
        public void MudAndChestSitOnEveryLaneAfterTheCommitPoint([Range(1, 8)] int seed)
        {
            foreach (var f in LevelPlanner.Create(Settings(seed)).Forks)
                foreach (var lane in f.Lanes)
                {
                    Assert.That(lane.MudLeft, Is.GreaterThan(f.CommitX));
                    Assert.That(lane.MudRight, Is.GreaterThan(lane.MudLeft + 2f));
                    Assert.That(lane.ChestX, Is.GreaterThan(lane.MudRight).And.LessThan(lane.Right - 0.5f));
                    Assert.That(f.CommitX, Is.GreaterThan(lane.Left + 0.5f), "everyone has landed before committing");
                }
        }

        // The approach is plain flat ground: nothing to jump, nothing in the way, no planned jump.
        [Test]
        public void ForkApproachIsFlatAndClear([Range(1, 8)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed));
            foreach (var f in plan.Forks)
            {
                float start = plan.Zones[f.ZoneIndex].X;
                for (float x = start; x < f.SplitX; x += 0.25f)
                    Assert.That(plan.GroundTopAt(x), Is.EqualTo(f.BaseTop).Within(1e-4f), $"flat approach at {x}");
                bool Inside(float x) => x >= start - 0.5f && x <= f.EndX;
                Assert.That(plan.Jumps.Any(j => Inside(j.X)), Is.False, "fork take-offs are not in the shared jump plan");
                Assert.That(plan.Spikes.Any(p => Inside(p.X)) || plan.Saws.Any(p => Inside(p.X)) || plan.Bees.Any(p => Inside(p.X)), Is.False);
                Assert.That(plan.Water.Any(w => w.Right >= start && w.Left <= f.EndX), Is.False);
                Assert.That(plan.Platforms.Any(p => p.Right >= start && p.Left <= f.EndX), Is.False, "no other ledges mixed in");
            }
        }

        [Test]
        public void NothingStandsInsideAFork([Range(1, 8)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed));
            foreach (var f in plan.Forks)
            {
                float start = f.SplitX - 2f, end = f.EndX + 1f;
                bool Inside(float x) => x >= start && x <= end;
                Assert.That(plan.Decorations.Any(d => Inside(d.X)), Is.False, "decorations");
                Assert.That(plan.Life.Any(l => Inside(l.X)), Is.False, "life");
                Assert.That(plan.Coins.Any(c => Inside(c.X)), Is.False, "coins");
                Assert.That(plan.Backdrop.Any(d => Inside(d.X)), Is.False, "backdrop");
            }
        }

        [Test]
        public void CheckpointAfterTheRejoin([Range(1, 8)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed));
            foreach (var f in plan.Forks)
            {
                var cp = plan.Checkpoints.First(c => c.X > plan.Zones[f.ZoneIndex].X);
                Assert.That(cp.X, Is.GreaterThan(f.EndX));
                Assert.That(cp.Y - 1f, Is.EqualTo(plan.GroundTopAt(cp.X)).Within(1e-4f));
            }
        }

        // Early Meadow levels teach the fork, later levels mix it with enemy encounters (user decision 2026-10-19).
        [Test]
        public void ShippedLevelsMixForksAndEncounters()
        {
            float[] expected = { 1f, 1f, 0.5f, 0.33f, 0.33f };
            var levels = LevelCatalog.Load().Levels;
            for (int i = 0; i < expected.Length; i++)
                Assert.That(levels[i].ForkShare, Is.EqualTo(expected[i]).Within(1e-3f), "level " + levels[i].LevelNumber);
        }

        // The slow motion starts when the fork is about a screen ahead, even with a long question time (Level 3 gives 12 s,
        // which used to start the slow-down 33 units — three screens — before the fork). The question's time is the
        // slowed run-up's real duration.
        [Test]
        public void SlowMotionStartsWithTheForkInView([Values(4f, 12f)] float limit, [Values(5f, 5.5f, 6f)] float speed)
        {
            var s = Settings(speed: speed);
            s.TimeLimit = limit;
            s.UntimedLeadSeconds = limit;
            foreach (var f in LevelPlanner.Create(s).Forks)
            {
                float approach = f.SplitX - LevelPlanner.Create(s).Zones[f.ZoneIndex].X;
                Assert.That(approach, Is.LessThanOrEqualTo(LevelPlanner.ForkMaxApproach + 1e-3f));
                Assert.That(f.ThinkSeconds, Is.EqualTo(approach / (speed * s.FocusScale)).Within(1e-3f));
                Assert.That(f.ThinkSeconds, Is.GreaterThanOrEqualTo(4f), "still time to read and decide");
            }
        }

        [Test]
        public void ZoneLengthCoversTheWholeFork([Range(1, 5)] int seed)
        {
            var s = Settings(seed, 0.5f);
            var plan = LevelPlanner.Create(s);
            foreach (var f in plan.Forks)
                Assert.That(plan.Zones[f.ZoneIndex].X + plan.ZoneLength(f.ZoneIndex, s), Is.EqualTo(f.EndX).Within(1e-3f));
        }
    }
}
