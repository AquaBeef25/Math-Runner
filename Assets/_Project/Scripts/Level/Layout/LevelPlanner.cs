using System.Collections.Generic;

namespace MathRunner.Level
{
    public struct LevelPlanSettings
    {
        public int QuestionCount;
        public float RunSpeed;
        public float TimedRatio;
        public float TimeLimit;
        public float UntimedLeadSeconds;
        // Seconds of an encounter at normal speed (enemy fly-in, wind-up, exit buffer).
        public float ZoneSpanSeconds;
        // World speed during the Focus Zone; the Lead seconds of slow-mo only cover Lead × FocusScale seconds of ground.
        public float FocusScale;
        public int HazardsBetweenQuestions;
        public float GapChance;
        public int Seed;

        // The player's jump (from PlayerController), so every piece is checked against what the player can really do.
        public float JumpHeight;
        public float Gravity;
        public float GravityScale;
        public float FallMultiplier;

        // World shape.
        public int MaxStepTiles;     // 0 = flat ground; 1–2 = steps and pillars up to that many tiles
        public int RouteSplits;      // high/low route splits per level
        public float PlatformChance; // chance a spike gets an optional platform over it
        public float WalkerChance;   // chance a hazard is a patrolling slime
        public float SawChance;      // chance a hazard is a saw

        // Spacing, in seconds of running (distance = seconds × run speed).
        public float IntroSeconds;
        public float HazardSpacingSeconds;
        public float GapSeconds;
        public float AfterZoneSeconds;
        public float OutroSeconds;

        public static LevelPlanSettings From(LevelConfig config, float zoneSpanSeconds, float focusScale,
            float jumpHeight, float gravity, float gravityScale, float fallMultiplier) => new LevelPlanSettings
        {
            FocusScale = focusScale,
            QuestionCount = config.QuestionCount,
            RunSpeed = config.RunSpeed,
            TimedRatio = config.TimedRatio,
            TimeLimit = config.TimeLimit,
            UntimedLeadSeconds = config.UntimedLeadSeconds,
            ZoneSpanSeconds = zoneSpanSeconds,
            HazardsBetweenQuestions = config.HazardsBetweenQuestions,
            GapChance = config.GapChance,
            Seed = config.LayoutSeed,
            JumpHeight = jumpHeight,
            Gravity = gravity,
            GravityScale = gravityScale,
            FallMultiplier = fallMultiplier,
            MaxStepTiles = config.MaxStepTiles,
            RouteSplits = config.RouteSplits,
            PlatformChance = config.PlatformChance,
            WalkerChance = config.WalkerChance,
            SawChance = config.SawChance,
            IntroSeconds = 2f,
            HazardSpacingSeconds = 1.6f,
            GapSeconds = 0.4f,
            AfterZoneSeconds = 3f,
            OutroSeconds = 2f,
        };
    }

    public sealed class LevelPlan
    {
        public readonly List<(float Start, float End, float Top)> Ground = new List<(float Start, float End, float Top)>();
        public readonly List<(float Left, float Right, float Top)> Platforms = new List<(float Left, float Right, float Top)>();
        public readonly List<(float X, float Y)> Spikes = new List<(float X, float Y)>();
        public readonly List<(float Left, float Right, float Y)> Walkers = new List<(float Left, float Right, float Y)>();
        public readonly List<(float X, float Y)> Saws = new List<(float X, float Y)>();
        public readonly List<(float X, bool Timed, float Lead)> Zones = new List<(float X, bool Timed, float Lead)>();
        public readonly List<(float X, float Y)> Checkpoints = new List<(float X, float Y)>();
        // One safe way through: press jump when crossing X, hold for HoldSeconds of scaled time.
        public readonly List<(float X, float HoldSeconds)> Jumps = new List<(float X, float HoldSeconds)>();
        public int Splits;
        public float FinishX;
        public float EndX;

        public float ZoneLength(int index, LevelPlanSettings s) => (Zones[index].Lead * s.FocusScale + s.ZoneSpanSeconds) * s.RunSpeed;

        // Height of the ground at x, or NaN over a gap.
        public float GroundTopAt(float x)
        {
            foreach (var g in Ground)
                if (g.Start <= x && x <= g.End) return g.Top;
            return float.NaN;
        }
    }

    // Lays out a level as: intro, then per question [hazards (maybe a route split) → encounter → recovery run], then the
    // finish. Hazards are spikes, gaps, steps, pillars and platform hops; encounters get gap-free terrain. Pure and
    // deterministic for a given seed, so layouts can be checked by EditMode tests.
    public static class LevelPlanner
    {
        public const float StartX = -10f;
        private const int MaxTop = 3;
        private const float SpikeLeadSeconds = 0.27f;
        private const float GapLeadSeconds = 0.1f;
        private const float ShortHold = 0.1f;
        private const float FullHold = 0.44f;
        private const float HalfPlayerWidth = 0.45f;
        private const float StepClearance = 0.3f;
        private const float PillarWidth = 1f;
        private const float PlatformWidth = 3f;
        public const float WalkerPatrol = 1.5f;

        private sealed class Builder
        {
            public readonly LevelPlan Plan = new LevelPlan();
            public readonly LevelPlanSettings S;
            public readonly System.Random Rng;
            public float X;
            public int Top;
            private float _groundStart = StartX;

            public Builder(LevelPlanSettings s)
            {
                S = s;
                Rng = new System.Random(s.Seed);
            }

            private float V => S.RunSpeed;

            public void SetTop(int top)
            {
                if (top == Top) return;
                Plan.Ground.Add((_groundStart, X, Top));
                _groundStart = X;
                Top = top;
            }

            public void Spike()
            {
                Plan.Spikes.Add((X, Top));
                Plan.Jumps.Add((X - SpikeLeadSeconds * V, ShortHold));
            }

            public void Gap()
            {
                float width = S.GapSeconds * V;
                Plan.Ground.Add((_groundStart, X, Top));
                Plan.Jumps.Add((X - GapLeadSeconds * V, FullHold));
                _groundStart = X + width;
                X += width;
            }

            // Ground rises by `tiles` at X; the jump leaves early enough to clear the edge with room to spare.
            public void StepUp(int tiles)
            {
                float lead = (JumpReach.RiseTime(S, tiles + StepClearance) + 0.08f) * V + HalfPlayerWidth;
                Plan.Jumps.Add((X - lead, FullHold));
                SetTop(Top + tiles);
            }

            public void StepDown(int tiles) => SetTop(Top - tiles);

            // A slime pacing 1.5 units on flat ground. One full jump stays above it for ~0.65 s, which clears the whole
            // patrol from this take-off at run speeds of 5 and up.
            public void Walker()
            {
                Plan.Walkers.Add((X, X + WalkerPatrol, Top));
                Plan.Jumps.Add((X - 0.85f - 0.05f * V, FullHold));
            }

            public void Saw()
            {
                Plan.Saws.Add((X, Top));
                Plan.Jumps.Add((X - SpikeLeadSeconds * V, FullHold));
            }

            public void Pillar(int tiles)
            {
                int ground = Top;
                StepUp(tiles);
                X += PillarWidth;
                SetTop(ground);
            }

            // An optional platform two tiles up over a spike: hop onto it or jump the spike.
            public void PlatformHop()
            {
                Plan.Platforms.Add((X - PlatformWidth * 0.5f, X + PlatformWidth * 0.5f, Top + 2));
                Spike();
            }

            // High route: three platforms (2, 3, 2 tiles up); low route: ground with a spike under the first and last.
            public void Split()
            {
                float x0 = X;
                Plan.Platforms.Add((x0, x0 + PlatformWidth, Top + 2));
                Plan.Platforms.Add((x0 + 4.5f, x0 + 4.5f + PlatformWidth, Top + 3));
                Plan.Platforms.Add((x0 + 9f, x0 + 9f + PlatformWidth, Top + 2));
                X = x0 + 1.5f;
                Spike();
                X = x0 + 10.5f;
                Spike();
                X = x0 + 12f;
                Plan.Splits++;
            }

            public int StepSize(int maxTiles) => 1 + Rng.Next(System.Math.Max(1, maxTiles));

            // Up or down by up to maxTiles, staying within [0, MaxTop].
            public void StepEitherWay(int maxTiles)
            {
                int tiles = StepSize(maxTiles);
                bool canUp = Top + tiles <= MaxTop;
                bool canDown = Top - tiles >= 0;
                if (canUp && (!canDown || Rng.NextDouble() < 0.5)) StepUp(tiles);
                else if (canDown) StepDown(tiles);
                else Spike();
            }

            // A pillar no taller than maxTiles that still ends within MaxTop; a spike if there's no room.
            public void PillarOrSpike(int maxTiles)
            {
                int tiles = System.Math.Min(StepSize(maxTiles), MaxTop - Top);
                if (tiles > 0) Pillar(tiles);
                else Spike();
            }

            public void Close(float endX) => Plan.Ground.Add((_groundStart, endX, Top));
        }

        public static LevelPlan Create(LevelPlanSettings s)
        {
            var b = new Builder(s);
            float v = s.RunSpeed;
            b.X = s.IntroSeconds * v;
            var splitAt = SplitQuestions(s.QuestionCount, s.RouteSplits);

            for (int q = 0; q < s.QuestionCount; q++)
            {
                if (splitAt.Contains(q))
                {
                    b.X += s.HazardSpacingSeconds * v;
                    b.Split();
                }
                for (int h = 0; h < s.HazardsBetweenQuestions; h++)
                {
                    b.X += s.HazardSpacingSeconds * v;
                    HazardBeat(b);
                }
                b.X += s.HazardSpacingSeconds * v;

                bool timed = s.TimeLimit > 0f && IsTimed(q, s.TimedRatio);
                float lead = timed ? s.TimeLimit : System.Math.Max(s.UntimedLeadSeconds, s.TimeLimit);
                float zoneStart = b.X;
                b.Plan.Zones.Add((zoneStart, timed, lead));
                float zoneEnd = zoneStart + (lead * s.FocusScale + s.ZoneSpanSeconds) * v;
                EncounterTerrain(b, zoneStart, zoneEnd);

                b.X = zoneEnd;
                b.Plan.Checkpoints.Add((b.X + 1f, b.Top + 1f));
                b.X += s.AfterZoneSeconds * v;
            }

            b.X += s.OutroSeconds * v;
            b.Plan.FinishX = b.X;
            b.Plan.EndX = b.X + 3f * v;
            b.Close(b.Plan.EndX);
            return b.Plan;
        }

        // One hazard between questions: gap, step, pillar, platform hop or spike, weighted by the level's knobs.
        private static void HazardBeat(Builder b)
        {
            var s = b.S;
            if (b.Rng.NextDouble() < s.GapChance)
            {
                b.Gap();
                return;
            }
            if (b.Rng.NextDouble() < s.WalkerChance)
            {
                b.Walker();
                return;
            }
            if (b.Rng.NextDouble() < s.SawChance)
            {
                b.Saw();
                return;
            }
            double roll = b.Rng.NextDouble();
            double stepChance = 0.25 * s.MaxStepTiles; // taller steps allowed → more height changes
            if (s.MaxStepTiles > 0 && roll < stepChance * 0.6) b.StepEitherWay(s.MaxStepTiles);
            else if (s.MaxStepTiles > 0 && roll < stepChance) b.PillarOrSpike(s.MaxStepTiles);
            else if (b.Rng.NextDouble() < s.PlatformChance) b.PlatformHop();
            else b.Spike();
        }

        // Inside an encounter: spikes, walkers, 1-tile steps and pillars only — never a gap, so a fall can't happen mid-question.
        private static void EncounterTerrain(Builder b, float start, float end)
        {
            var s = b.S;
            float v = s.RunSpeed;
            b.X = start + 0.8f * v;
            while (b.X < end - 2.5f)
            {
                double roll = b.Rng.NextDouble();
                if (b.Rng.NextDouble() < s.WalkerChance) b.Walker();
                else if (s.MaxStepTiles > 0 && roll < 0.3) b.StepEitherWay(1);
                else if (s.MaxStepTiles > 0 && roll < 0.45) b.PillarOrSpike(1);
                else b.Spike();
                b.X += s.HazardSpacingSeconds * v;
            }
        }

        // Spreads route splits evenly over the questions.
        private static HashSet<int> SplitQuestions(int questions, int splits)
        {
            var set = new HashSet<int>();
            for (int i = 1; i <= splits && questions > 0; i++)
                set.Add(System.Math.Min(questions - 1, (int)System.Math.Round(i * (double)questions / (splits + 1))));
            return set;
        }

        // Spreads timed questions evenly: with ratio 0.5 every second question is timed, starting with the second.
        public static bool IsTimed(int questionIndex, float ratio) =>
            System.Math.Floor((questionIndex + 1) * ratio + 1e-4) > System.Math.Floor(questionIndex * ratio + 1e-4);
    }
}
