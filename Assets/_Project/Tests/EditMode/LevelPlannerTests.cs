using System.Linq;
using MathRunner.Level;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class LevelPlannerTests
    {
        private static LevelPlanSettings Settings(int count = 8, float speed = 6f, float ratio = 0.5f, float limit = 4f,
            int hazards = 2, float gapChance = 0.3f, int seed = 1, int maxStep = 2, int splits = 2, float platforms = 0.5f, float walkers = 0f, float saws = 0f, float decor = 0f, float ramps = 0f, float water = 0f, float bees = 0f, float coins = 0f, bool backdrop = false) => new LevelPlanSettings
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
            WalkerChance = walkers,
            SawChance = saws,
            DecorationDensity = decor,
            RampChance = ramps,
            WaterChance = water,
            BeeChance = bees,
            CoinChance = coins,
            BackdropWeights = backdrop ? new[] { 3f, 2f, 2f, 1f, 0.3f } : null,
            DecorationWeights = new[] { 3f, 3f, 1f, 1f, 1f, 0.5f, 0.5f },
        };

        private static bool OnGround(LevelPlan plan, float from, float to) =>
            plan.Ground.Any(g => g.Start <= from && to <= g.End);

        // A ramp bridges the joint between two flat stretches; that joint is not a gap.
        private static bool IsRampJoint(LevelPlan plan, float end, float start) =>
            plan.Ramps.Any(r => System.Math.Abs(r.Start - end) < 1e-3f && System.Math.Abs(r.End - start) < 1e-3f);

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
                if (IsRampJoint(plan, a.End, b.Start)) continue;
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
                if (IsRampJoint(plan, plan.Ground[i].End, plan.Ground[i + 1].Start)) continue;
                bool gapOrRise = plan.Ground[i + 1].Start - plan.Ground[i].End > 0.01f || plan.Ground[i + 1].Top > plan.Ground[i].Top;
                if (!gapOrRise) continue;
                float edge = plan.Ground[i].End;
                Assert.That(plan.Jumps.Any(j => j.X < edge && edge - j.X < 0.6f * s.RunSpeed), Is.True, $"No jump planned before the edge at {edge}");
            }
        }

        [Test]
        public void WalkersStayOnFlatGround([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed, walkers: 0.6f);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.Walkers, Is.Not.Empty);
            foreach (var w in plan.Walkers)
            {
                Assert.That(w.Right - w.Left, Is.EqualTo(1.5f).Within(1e-4f));
                for (float x = w.Left - 0.5f; x <= w.Right + 0.5f; x += 0.25f)
                    Assert.That(plan.GroundTopAt(x), Is.EqualTo(w.Y).Within(1e-4f), $"Walker at {w.Left} isn't on flat ground at {x}");
                Assert.That(plan.Jumps.Any(j => j.X < w.Left && w.Left - j.X < 0.6f * s.RunSpeed), Is.True, $"No jump planned before the walker at {w.Left}");
            }
        }

        [Test]
        public void SawsSitOnASurface([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed, saws: 0.6f);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.Saws, Is.Not.Empty);
            foreach (var sw in plan.Saws)
            {
                Assert.That(sw.Y, Is.EqualTo(plan.GroundTopAt(sw.X)).Within(1e-4f), $"Saw at {sw.X} floats or is buried");
                Assert.That(plan.Jumps.Any(j => j.X < sw.X && sw.X - j.X < 0.6f * s.RunSpeed), Is.True, $"No jump planned before the saw at {sw.X}");
            }
        }

        [Test]
        public void EveryBeeHasAJumpBeforeIt([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed, bees: 0.6f);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.Bees, Is.Not.Empty);
            foreach (var bee in plan.Bees)
                Assert.That(plan.Jumps.Any(j => j.X < bee.X && bee.X - j.X < 0.6f * s.RunSpeed && j.HoldSeconds > 0.3f), Is.True,
                    $"No full jump planned before the bee at {bee.X}");
        }

        // When the runner's front edge (half-width 0.45) reaches the bee's left edge (radius 0.35), its feet must already be
        // above the bee at the top of its bob.
        [Test]
        public void BeeJumpClearsTheBee([Values(5f, 6f, 8f)] float speed, [Range(1, 10)] int seed)
        {
            var s = Settings(speed: speed, seed: seed, bees: 0.6f);
            var plan = LevelPlanner.Create(s);
            double g = System.Math.Abs(s.Gravity) * s.GravityScale, v0 = System.Math.Sqrt(2.0 * g * s.JumpHeight);
            foreach (var bee in plan.Bees)
            {
                var jump = plan.Jumps.Where(j => j.X < bee.X).OrderBy(j => j.X).Last();
                double t = (bee.X - 0.35 - 0.45 - jump.X) / s.RunSpeed;
                double feet = plan.GroundTopAt(jump.X) + v0 * t - 0.5 * g * t * t;
                Assert.That(feet, Is.GreaterThanOrEqualTo(bee.BaseY + bee.Amplitude + 0.35), $"Runner at speed {speed} clips the bee at {bee.X}");
            }
        }

        [Test]
        public void BeesFlyOverFlatGround([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, bees: 0.6f));
            foreach (var bee in plan.Bees)
            {
                float top = plan.GroundTopAt(bee.X);
                Assert.That(bee.BaseY, Is.EqualTo(top + 0.9f).Within(1e-4f), $"Bee at {bee.X} isn't 0.9 above the ground");
                Assert.That(bee.Amplitude, Is.EqualTo(0.4f).Within(1e-4f));
                for (float x = bee.X - 0.5f; x <= bee.X + 0.5f; x += 0.1f)
                    Assert.That(plan.GroundTopAt(x), Is.EqualTo(top).Within(1e-4f), $"Ground under the bee at {bee.X} isn't flat at {x}");
            }
        }

        [Test]
        public void CoinsSitOnSafeArcs([Values(5f, 6f, 8f)] float speed, [Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(speed: speed, seed: seed, gapChance: 0.4f, walkers: 0.3f, bees: 0.3f, coins: 1f));
            Assert.That(plan.Coins, Is.Not.Empty);
            foreach (var c in plan.Coins)
            {
                float ground = plan.GroundTopAt(c.X);
                if (!float.IsNaN(ground)) Assert.That(c.Y - ground, Is.GreaterThanOrEqualTo(0.5f), $"Coin at {c.X} is in the ground");
                foreach (var p in plan.Spikes) Assert.That(Dist(c, p.X, p.Y + 0.4f), Is.GreaterThanOrEqualTo(1.5f), $"Coin at {c.X} is next to a spike");
                foreach (var p in plan.Saws) Assert.That(Dist(c, p.X, p.Y + 0.4f), Is.GreaterThanOrEqualTo(1.5f), $"Coin at {c.X} is next to a saw");
                foreach (var p in plan.Bees) Assert.That(Dist(c, p.X, p.BaseY), Is.GreaterThanOrEqualTo(1.5f + p.Amplitude), $"Coin at {c.X} is next to a bee");
                foreach (var w in plan.Walkers)
                {
                    float dx = System.Math.Max(0f, System.Math.Max(w.Left - c.X, c.X - w.Right));
                    float dy = System.Math.Max(0f, c.Y - (w.Y + 1f));
                    Assert.That(System.Math.Sqrt(dx * dx + dy * dy), Is.GreaterThanOrEqualTo(1.5f), $"Coin at {c.X} is next to a walker");
                }
            }
        }

        private static double Dist((float X, float Y) c, float x, float y) =>
            System.Math.Sqrt((c.X - x) * (c.X - x) + (c.Y - y) * (c.Y - y));

        [Test]
        public void SameSeedSameCoins([Range(1, 5)] int seed)
        {
            var s = Settings(seed: seed, gapChance: 0.4f, walkers: 0.3f, bees: 0.3f, coins: 0.5f);
            Assert.That(LevelPlanner.Create(s).Coins, Is.EqualTo(LevelPlanner.Create(s).Coins));
        }

        [Test]
        public void CoinsNeverChangeTheTerrain([Range(1, 5)] int seed)
        {
            var without = LevelPlanner.Create(Settings(seed: seed, bees: 0.3f));
            var with = LevelPlanner.Create(Settings(seed: seed, bees: 0.3f, coins: 1f));
            Assert.That(with.Ground, Is.EqualTo(without.Ground));
            Assert.That(with.Jumps, Is.EqualTo(without.Jumps));
        }

        [Test]
        public void BackdropPropsSitOnTheGround([Values(5f, 8f)] float speed, [Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(speed: speed, seed: seed, walkers: 0.2f, saws: 0.2f, bees: 0.2f, ramps: 0.5f, backdrop: true));
            Assert.That(plan.Backdrop, Is.Not.Empty);
            foreach (var p in plan.Backdrop)
            {
                Assert.That(plan.Ground.Any(g => g.Start + 1f <= p.X && p.X <= g.End - 1f && System.Math.Abs(g.Top - p.Y) < 1e-4f), Is.True,
                    $"Backdrop prop at {p.X} isn't on a flat ground top");
                Assert.That(p.Kind, Is.InRange(0, 4));
                foreach (float x in plan.Spikes.Select(h => h.X).Concat(plan.Saws.Select(h => h.X)).Concat(plan.Bees.Select(h => h.X)))
                    Assert.That(System.Math.Abs(p.X - x), Is.GreaterThanOrEqualTo(1.5f), $"Backdrop prop at {p.X} sits behind a hazard at {x}");
                foreach (var w in plan.Walkers)
                    Assert.That(p.X < w.Left - 1.5f || p.X > w.Right + 1.5f, Is.True, $"Backdrop prop at {p.X} is behind a walker");
            }
            for (int i = 1; i < plan.Backdrop.Count; i++)
                Assert.That(plan.Backdrop[i].X - plan.Backdrop[i - 1].X, Is.GreaterThanOrEqualTo(4f - 1e-4f), "props are at least 4 units apart");
        }

        [Test]
        public void SameSeedSameBackdrop([Range(1, 5)] int seed)
        {
            var s = Settings(seed: seed, backdrop: true);
            Assert.That(LevelPlanner.Create(s).Backdrop, Is.EqualTo(LevelPlanner.Create(s).Backdrop));
        }

        [Test]
        public void DecorationsKeepClearOfHazards([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed, walkers: 0.3f, saws: 0.2f, decor: 2.5f);
            var plan = LevelPlanner.Create(s);
            Assert.That(plan.Decorations, Is.Not.Empty);
            var gapEdges = plan.Ground.Zip(plan.Ground.Skip(1), (a, b) => b.Start - a.End > 0.01f && !IsRampJoint(plan, a.End, b.Start) ? new[] { a.End, b.Start } : new float[0]).SelectMany(e => e);
            var keepClear = plan.Spikes.Select(p => p.X)
                .Concat(plan.Saws.Select(p => p.X))
                .Concat(plan.Jumps.Select(j => j.X))
                .Concat(gapEdges)
                .ToList();
            foreach (var d in plan.Decorations)
            {
                foreach (float x in keepClear)
                    Assert.That(System.Math.Abs(d.X - x), Is.GreaterThanOrEqualTo(1.5f), $"Decoration at {d.X} is too close to {x}");
                foreach (var w in plan.Walkers)
                    Assert.That(d.X < w.Left - 1.5f || d.X > w.Right + 1.5f, Is.True, $"Decoration at {d.X} is in a walker's patrol");
            }
        }

        [Test]
        public void DecorationsSitOnTheGroundTop([Range(1, 10)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, decor: 2.5f));
            foreach (var d in plan.Decorations)
            {
                Assert.That(OnGround(plan, d.X - 0.6f, d.X + 0.6f), Is.True, $"Decoration at {d.X} hangs over an edge");
                Assert.That(d.Y, Is.EqualTo(plan.GroundTopAt(d.X)).Within(1e-4f));
                Assert.That(d.Kind, Is.InRange(0, 6));
            }
        }

        [Test]
        public void DecorationDensityIsRoughlyAsSet([Range(1, 10)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, decor: 2.5f));
            float groundLength = plan.Ground.Sum(g => g.End - g.Start);
            float perTen = plan.Decorations.Count / groundLength * 10f;
            Assert.That(perTen, Is.InRange(2.5f * 0.6f, 2.5f * 1.4f));
        }

        [Test]
        public void DecorationsDontChangeTheTerrain()
        {
            var bare = LevelPlanner.Create(Settings(seed: 3));
            var decorated = LevelPlanner.Create(Settings(seed: 3, decor: 2.5f));
            Assert.That(decorated.Ground, Is.EqualTo(bare.Ground));
            Assert.That(decorated.Spikes, Is.EqualTo(bare.Spikes));
            Assert.That(decorated.Jumps, Is.EqualTo(bare.Jumps));
        }

        [Test]
        public void SameSeedSameDecorations()
        {
            var a = LevelPlanner.Create(Settings(seed: 5, decor: 2.5f));
            var b = LevelPlanner.Create(Settings(seed: 5, decor: 2.5f));
            Assert.That(b.Decorations, Is.EqualTo(a.Decorations));
        }

        [Test]
        public void RampsAreGentleAndJoinTheGround([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, ramps: 0.7f));
            foreach (var r in plan.Ramps)
            {
                Assert.That(System.Math.Abs(r.ToTop - r.FromTop) / (r.End - r.Start), Is.LessThanOrEqualTo(0.5f + 1e-4f), $"Ramp at {r.Start} is too steep");
                Assert.That(plan.GroundTopAt(r.Start - 0.01f), Is.EqualTo(r.FromTop).Within(1e-3f), $"Ramp at {r.Start} doesn't start on its ground");
                Assert.That(plan.GroundTopAt(r.End + 0.01f), Is.EqualTo(r.ToTop).Within(1e-3f), $"Ramp at {r.Start} doesn't end on its ground");
            }
        }

        [Test]
        public void GroundTopIsContinuousAcrossRamps([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, ramps: 0.7f));
            foreach (var r in plan.Ramps)
            {
                float previous = plan.GroundTopAt(r.Start - 1f);
                for (float x = r.Start - 0.9f; x <= r.End + 1f; x += 0.1f)
                {
                    float top = plan.GroundTopAt(x);
                    Assert.That(float.IsNaN(top), Is.False, $"Hole at {x} near the ramp at {r.Start}");
                    Assert.That(System.Math.Abs(top - previous), Is.LessThanOrEqualTo(0.051f), $"Ground jumps at {x} on the ramp at {r.Start}");
                    previous = top;
                }
            }
        }

        [Test]
        public void NoHazardOnARamp([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, ramps: 0.7f, walkers: 0.3f, saws: 0.2f, decor: 2.5f));
            var things = plan.Spikes.Select(p => p.X).Concat(plan.Saws.Select(p => p.X)).Concat(plan.Decorations.Select(d => d.X))
                .Concat(plan.Walkers.SelectMany(w => new[] { w.Left, w.Right }));
            foreach (var r in plan.Ramps)
                foreach (float x in things)
                    Assert.That(x < r.Start - 0.5f || x > r.End + 0.5f, Is.True, $"Something at {x} sits on the ramp {r.Start}-{r.End}");
        }

        [Test]
        public void LevelsHaveRamps([Range(1, 10)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, maxStep: 1, ramps: 0.6f));
            Assert.That(plan.Ramps.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void PillarLedgesStandOnFlatGround([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, splits: 3, platforms: 0.6f, ramps: 0.6f));
            Assert.That(plan.PlatformStyles.Count, Is.EqualTo(plan.Platforms.Count));
            for (int i = 0; i < plan.Platforms.Count; i++)
            {
                if (plan.PlatformStyles[i] != PlatformStyle.PillarLedge) continue;
                var p = plan.Platforms[i];
                float ground = plan.GroundTopAt(p.Left);
                for (float x = p.Left; x <= p.Right; x += 0.25f)
                    Assert.That(plan.GroundTopAt(x), Is.EqualTo(ground).Within(1e-4f), $"Pillar ledge at {p.Left} isn't over flat ground");
            }
        }

        [Test]
        public void EveryStyleAppearsAcrossSeeds()
        {
            var seen = new System.Collections.Generic.HashSet<PlatformStyle>();
            for (int seed = 1; seed <= 20; seed++)
                foreach (var style in LevelPlanner.Create(Settings(seed: seed, splits: 3, platforms: 0.6f)).PlatformStyles) seen.Add(style);
            Assert.That(seen, Is.EquivalentTo(System.Enum.GetValues(typeof(PlatformStyle))));
        }

        [Test]
        public void SameSeedSameStyles()
        {
            var a = LevelPlanner.Create(Settings(seed: 9, splits: 3, platforms: 0.6f));
            var b = LevelPlanner.Create(Settings(seed: 9, splits: 3, platforms: 0.6f));
            Assert.That(b.PlatformStyles, Is.EqualTo(a.PlatformStyles));
        }

        [Test]
        public void StylesDontChangeTheTerrain()
        {
            var a = LevelPlanner.Create(Settings(seed: 4, splits: 3, platforms: 0.6f));
            Assert.That(a.Platforms.Count, Is.GreaterThan(0));
            // Same layout as before styles existed: the style pass uses its own random stream.
            var b = LevelPlanner.Create(Settings(seed: 4, splits: 3, platforms: 0.6f));
            Assert.That(b.Ground, Is.EqualTo(a.Ground));
            Assert.That(b.Jumps, Is.EqualTo(a.Jumps));
        }

        [Test]
        public void WaterOnlyFillsGaps([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed: seed, gapChance: 0.6f, water: 0.7f));
            foreach (var w in plan.Water)
            {
                Assert.That(plan.Ground.Any(g => System.Math.Abs(g.End - w.Left) < 1e-3f), Is.True, $"Water at {w.Left} doesn't start at a ground edge");
                Assert.That(plan.Ground.Any(g => System.Math.Abs(g.Start - w.Right) < 1e-3f), Is.True, $"Water at {w.Left} doesn't end at a ground edge");
                Assert.That(float.IsNaN(plan.GroundTopAt((w.Left + w.Right) * 0.5f)), Is.True, $"Water at {w.Left} isn't in a pit");
                Assert.That(w.Surface, Is.LessThan(plan.GroundTopAt(w.Left - 0.01f) - 0.4f), "water surface must sit below the ground");
            }
        }

        [Test]
        public void SomeGapsBecomeWater()
        {
            int water = 0;
            for (int seed = 1; seed <= 10; seed++) water += LevelPlanner.Create(Settings(seed: seed, gapChance: 0.6f, water: 0.7f)).Water.Count;
            Assert.That(water, Is.GreaterThan(0));
        }

        [Test]
        public void NoWaterInEncounters([Range(1, 20)] int seed)
        {
            var s = Settings(seed: seed, gapChance: 0.6f, water: 0.9f);
            var plan = LevelPlanner.Create(s);
            for (int i = 0; i < plan.Zones.Count; i++)
            {
                float start = plan.Zones[i].X, end = start + plan.ZoneLength(i, s);
                Assert.That(plan.Water.Any(w => w.Right > start && w.Left < end), Is.False, $"Water inside encounter {i}");
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
