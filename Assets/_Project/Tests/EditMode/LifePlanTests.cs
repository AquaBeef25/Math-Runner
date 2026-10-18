using System.Linq;
using MathRunner.Level;
using NUnit.Framework;

namespace MathRunner.Tests
{
    // Harmless moving life in the level plan: where it may go, and that it never changes anything else.
    public class LifePlanTests
    {
        private static readonly float[] AllKinds = { 1f, 1f, 1f, 1f, 1f, 1f };
        private static readonly int[] Variants = { 2, 1, 2, 3, 1, 2 };

        private static LevelPlanSettings Settings(int seed = 1, float speed = 6f, float[] life = null, float fish = 1f,
            float water = 0.5f) => new LevelPlanSettings
        {
            QuestionCount = 8,
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
            RouteSplits = 2,
            PlatformChance = 0.5f,
            WalkerChance = 0.2f,
            SawChance = 0.1f,
            BeeChance = 0.2f,
            RampChance = 0.4f,
            WaterChance = water,
            CoinChance = 0.5f,
            DecorationDensity = 1f,
            DecorationWeights = new[] { 3f, 3f, 1f },
            BackdropWeights = new[] { 1f, 1f },
            LifeDensity = 0.6f,
            LifeWeights = life,
            LifeVariants = Variants,
            FishChance = fish,
        };

        private static float TotalGround(LevelPlan p) => p.Ground.Sum(g => g.End - g.Start);

        private static bool OnGround(LifeKind k) => k == LifeKind.Walker || k == LifeKind.Hopper || k == LifeKind.Flag || k == LifeKind.Torch;

        [Test]
        public void SameSeedSameLife([Range(1, 5)] int seed)
        {
            var s = Settings(seed, life: AllKinds);
            var a = LevelPlanner.Create(s);
            var b = LevelPlanner.Create(s);
            Assert.That(a.Life, Is.Not.Empty);
            Assert.That(a.Life, Is.EqualTo(b.Life));
            Assert.That(a.Fish, Is.EqualTo(b.Fish));
        }

        [Test]
        public void LifeNeverChangesThePlan([Range(1, 5)] int seed)
        {
            var with = LevelPlanner.Create(Settings(seed, life: AllKinds));
            var without = LevelPlanner.Create(Settings(seed, life: null));
            Assert.That(with.Ground, Is.EqualTo(without.Ground));
            Assert.That(with.Jumps, Is.EqualTo(without.Jumps));
            Assert.That(with.Coins, Is.EqualTo(without.Coins));
            Assert.That(with.Decorations, Is.EqualTo(without.Decorations));
            Assert.That(with.Backdrop, Is.EqualTo(without.Backdrop));
            Assert.That(with.Spikes, Is.EqualTo(without.Spikes));
            Assert.That(with.Bees, Is.EqualTo(without.Bees));
            Assert.That(with.Walkers, Is.EqualTo(without.Walkers));
        }

        [Test]
        public void GroundLifeKeepsClearOfHazards([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed, life: AllKinds));
            var hazards = plan.Spikes.Select(h => h.X).Concat(plan.Saws.Select(h => h.X)).Concat(plan.Bees.Select(h => h.X))
                .Concat(plan.Jumps.Select(j => j.X))
                .Concat(plan.Water.SelectMany(w => new[] { w.Left, w.Right }))
                .Concat(plan.Ground.Zip(plan.Ground.Skip(1), (a, b) => b.Start - a.End > 0.01f && !plan.IsRampJoint(a.End, b.Start) ? new[] { a.End, b.Start } : new float[0]).SelectMany(e => e))
                .ToList();
            foreach (var l in plan.Life.Where(l => OnGround(l.Kind)))
            {
                float right = l.Kind == LifeKind.Walker ? l.X + LevelPlanner.LifePatrol : l.X;
                Assert.That(plan.Ground.Any(g => g.Start + 1f <= l.X && right <= g.End - 1f && System.Math.Abs(g.Top - l.Y) < 1e-4f), Is.True,
                    $"{l.Kind} at {l.X} isn't on one flat ground top");
                foreach (float h in hazards)
                    Assert.That(h < l.X - 1.5f || h > right + 1.5f, Is.True, $"{l.Kind} at {l.X} is within 1.5 of {h}");
                foreach (var w in plan.Walkers)
                    Assert.That(w.Right < l.X - 1.5f || w.Left > right + 1.5f, Is.True, $"{l.Kind} at {l.X} overlaps a slime patrol");
            }
        }

        [Test]
        public void FlyersKeepClearOfJumpArcsAndCoins([Range(1, 20)] int seed)
        {
            var s = Settings(seed, life: AllKinds);
            var plan = LevelPlanner.Create(s);
            float arc = JumpReach.AirTime(s, 0f) * s.RunSpeed;
            foreach (var l in plan.Life.Where(l => l.Kind == LifeKind.Flyer))
            {
                foreach (var j in plan.Jumps.Where(j => j.HoldSeconds > 0.3f))
                    Assert.That(l.X < j.X - 1f || l.X > j.X + arc + 1f, Is.True, $"Flyer at {l.X} is in the jump arc from {j.X}");
                foreach (var c in plan.Coins)
                    Assert.That(System.Math.Sqrt((c.X - l.X) * (c.X - l.X) + (c.Y - l.Y) * (c.Y - l.Y)), Is.GreaterThanOrEqualTo(1.5), $"Flyer at {l.X} is next to a coin");
                float above = l.Y - plan.GroundTopAt(l.X);
                Assert.That(above, Is.InRange(2.5f - 1e-4f, 3.5f + 1e-4f), $"Flyer at {l.X} flies at {above}");
            }
        }

        // Review fix: a swinging crate never hangs where the runner jumps, rides the high route, or over an encounter.
        [Test]
        public void SwingsStayOutOfJumpArcsPlatformsAndEncounters([Range(1, 20)] int seed)
        {
            var s = Settings(seed, life: new[] { 0f, 0f, 0f, 0f, 0f, 1f });
            var plan = LevelPlanner.Create(s);
            float arc = JumpReach.AirTime(s, 0f) * s.RunSpeed;
            Assert.That(plan.Life, Is.Not.Empty);
            foreach (var l in plan.Life)
            {
                foreach (var j in plan.Jumps.Where(j => j.HoldSeconds > 0.3f))
                    Assert.That(l.X < j.X - 1f || l.X > j.X + arc + 1f, Is.True, $"Swing at {l.X} is in the jump arc from {j.X}");
                foreach (var p in plan.Platforms)
                    Assert.That(l.X < p.Left - 2f || l.X > p.Right + 2f, Is.True, $"Swing at {l.X} hangs over the platform {p.Left}-{p.Right}");
                for (int i = 0; i < plan.Zones.Count; i++)
                    Assert.That(l.X < plan.Zones[i].X - 2f || l.X > plan.Zones[i].X + plan.ZoneLength(i, s) + 2f, Is.True, $"Swing at {l.X} hangs over encounter {i}");
            }
        }

        // Review fix: a flyer's whole loop (x ± 0.8, y ± 0.4) stays 1.5 away from every floating platform.
        [Test]
        public void FlyersKeepClearOfPlatforms([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed, life: new[] { 0f, 0f, 1f, 0f, 0f, 0f }));
            foreach (var l in plan.Life)
                foreach (var p in plan.Platforms)
                {
                    float dx = System.Math.Max(0f, System.Math.Max(p.Left - (l.X + 0.8f), (l.X - 0.8f) - p.Right));
                    float dy = System.Math.Max(0f, System.Math.Max(p.Top - 0.5f - (l.Y + 0.4f), (l.Y - 0.4f) - (p.Top + 1f)));
                    Assert.That(System.Math.Sqrt(dx * dx + dy * dy), Is.GreaterThanOrEqualTo(1.5), $"Flyer at {l.X},{l.Y} is next to the platform {p.Left}-{p.Right}");
                }
        }

        // Review fix: critters, flags and torches never stand over a low coin.
        [Test]
        public void GroundLifeKeepsClearOfCoins([Range(1, 20)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed, life: new[] { 1f, 1f, 0f, 1f, 1f, 0f }));
            foreach (var l in plan.Life)
            {
                float right = l.Kind == LifeKind.Walker ? l.X + LevelPlanner.LifePatrol : l.X;
                foreach (var c in plan.Coins.Where(c => c.Y - l.Y < 2.5f))
                    Assert.That(c.X < l.X - 1.5f || c.X > right + 1.5f, Is.True, $"{l.Kind} at {l.X} stands over the coin at {c.X},{c.Y}");
            }
        }

        [Test]
        public void LifeIsSparse([Range(1, 10)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed, life: AllKinds));
            Assert.That(plan.Life.Count, Is.LessThanOrEqualTo((int)System.Math.Round(0.6 * TotalGround(plan) / 10.0)));
            var xs = plan.Life.Select(l => l.X).OrderBy(x => x).ToList();
            for (int i = 1; i < xs.Count; i++)
                Assert.That(xs[i] - xs[i - 1], Is.GreaterThanOrEqualTo(8f - 1e-4f), "life items are at least 8 units apart");
        }

        [Test]
        public void KindsWithZeroWeightNeverAppear([Range(1, 10)] int seed)
        {
            var plan = LevelPlanner.Create(Settings(seed, life: new[] { 1f, 0f, 1f, 0f, 1f, 0f }));
            Assert.That(plan.Life, Is.Not.Empty);
            Assert.That(plan.Life.Any(l => l.Kind == LifeKind.Hopper || l.Kind == LifeKind.Flag || l.Kind == LifeKind.Swing), Is.False);
            Assert.That(plan.Life.All(l => l.Variant >= 0 && l.Variant < Variants[(int)l.Kind]), Is.True);
        }

        [Test]
        public void SettingsTakeLifeFromTheLevel()
        {
            var level = UnityEngine.ScriptableObject.CreateInstance<LevelConfig>();
            var set = UnityEngine.ScriptableObject.CreateInstance<LifeSet>();
            var sprite = UnityEngine.Sprite.Create(new UnityEngine.Texture2D(4, 4), new UnityEngine.Rect(0, 0, 4, 4), UnityEngine.Vector2.zero);
            set.SetKind(LifeKind.Walker, 2f, new LifeSet.Variant(sprite, sprite), new LifeSet.Variant(sprite, sprite));
            set.SetKind(LifeKind.Flyer, 1f, new LifeSet.Variant(sprite));
            set.SetFish(0.5f, new LifeSet.Variant(sprite));
            level.SetLife(set);

            var s = LevelPlanSettings.From(level, 2f, 0.5f, 3.2f, -9.81f, 4f, 1.4f);
            Assert.That(s.LifeWeights, Is.EqualTo(set.Weights));
            Assert.That(s.LifeVariants[(int)LifeKind.Walker], Is.EqualTo(2));
            Assert.That(s.FishChance, Is.EqualTo(0.5f));

            level.SetLife(null);
            Assert.That(LevelPlanSettings.From(level, 2f, 0.5f, 3.2f, -9.81f, 4f, 1.4f).LifeWeights, Is.Null);
            UnityEngine.Object.DestroyImmediate(set);
            UnityEngine.Object.DestroyImmediate(level);
        }

        [Test]
        public void NoLifeWithoutWeights()
        {
            var plan = LevelPlanner.Create(Settings(life: null));
            Assert.That(plan.Life, Is.Empty);
            Assert.That(plan.Fish, Is.Empty);
        }

        [Test]
        public void FishOnlyInWaterPools()
        {
            int fish = 0;
            for (int seed = 1; seed <= 5; seed++)
            {
                var plan = LevelPlanner.Create(Settings(seed, life: AllKinds, fish: 1f, water: 0.5f));
                foreach (var f in plan.Fish) Assert.That(plan.Water, Does.Contain(f));
                fish += plan.Fish.Count;
            }
            Assert.That(fish, Is.GreaterThan(0));
        }
    }
}
