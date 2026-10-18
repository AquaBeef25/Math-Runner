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
        public float RampChance;     // chance a height change is a ramp instead of a step
        public float WaterChance;    // chance a pit is filled with water
        public float BeeChance;      // chance a hazard is a hovering bee
        public float CoinChance;     // chance a full jump over a gap, slime or bee gets an arc of coins
        public bool NoSpikes;        // spikes switched off: where one would go the ground stays plain (no jump planned)
        public float DecorationDensity;   // decorations per 10 units of ground (0 = none)
        public float[] DecorationWeights; // relative chance of each decoration kind
        public float[] BackdropWeights;   // relative chance of each backdrop prop (trees, bushes, fences, houses); null = none

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
            RampChance = config.RampChance,
            WaterChance = config.WaterChance,
            BeeChance = config.BeeChance,
            CoinChance = config.CoinChance,
            NoSpikes = config.NoSpikes,
            DecorationDensity = config.Decorations != null ? config.DecorationDensity : 0f,
            DecorationWeights = config.Decorations != null ? config.Decorations.Weights : null,
            BackdropWeights = config.Decorations != null ? config.Decorations.BackdropWeights : null,
            IntroSeconds = 2f,
            HazardSpacingSeconds = 1.6f,
            GapSeconds = 0.4f,
            AfterZoneSeconds = 3f,
            OutroSeconds = 2f,
        };
    }

    // How a floating platform looks; all styles are the same one-way ledge to the player.
    public enum PlatformStyle { Ledge, PillarLedge, Bridge, Blocks }

    public sealed class LevelPlan
    {
        public readonly List<(float Start, float End, float Top)> Ground = new List<(float Start, float End, float Top)>();
        // Sloped joints between two flat stretches (2 units of run per tile of rise); no jump needed.
        public readonly List<(float Start, float End, float FromTop, float ToTop)> Ramps = new List<(float Start, float End, float FromTop, float ToTop)>();
        public readonly List<(float Left, float Right, float Top)> Platforms = new List<(float Left, float Right, float Top)>();
        public readonly List<PlatformStyle> PlatformStyles = new List<PlatformStyle>(); // index-aligned with Platforms
        public readonly List<(float X, float Y)> Spikes = new List<(float X, float Y)>();
        public readonly List<(float Left, float Right, float Y)> Walkers = new List<(float Left, float Right, float Y)>();
        // Water filling a pit: Left/Right are the pit's edges, Surface sits below the ground.
        public readonly List<(float Left, float Right, float Surface)> Water = new List<(float Left, float Right, float Surface)>();
        public readonly List<(float X, float Y)> Saws = new List<(float X, float Y)>();
        // Bees hover at BaseY (head height over flat ground) and bob by Amplitude.
        public readonly List<(float X, float BaseY, float Amplitude)> Bees = new List<(float X, float BaseY, float Amplitude)>();
        // Coin centres: arcs along planned jumps and rows over the high route. Points only, never part of the terrain.
        public readonly List<(float X, float Y)> Coins = new List<(float X, float Y)>();
        // Scenery standing just behind the play area (trees, bushes, fences, houses), on flat ground tops.
        public readonly List<(float X, float Y, int Kind)> Backdrop = new List<(float X, float Y, int Kind)>();
        public readonly List<(float X, bool Timed, float Lead)> Zones = new List<(float X, bool Timed, float Lead)>();
        public readonly List<(float X, float Y, int Kind)> Decorations = new List<(float X, float Y, int Kind)>();
        public readonly List<(float X, float Y)> Checkpoints = new List<(float X, float Y)>();
        // One safe way through: press jump when crossing X, hold for HoldSeconds of real time (same arc in slow-mo).
        public readonly List<(float X, float HoldSeconds)> Jumps = new List<(float X, float HoldSeconds)>();
        public int Splits;
        public float FinishX;
        public float EndX;

        public float ZoneLength(int index, LevelPlanSettings s) => (Zones[index].Lead * s.FocusScale + s.ZoneSpanSeconds) * s.RunSpeed;

        // Height of the ground at x (sloped on ramps), or NaN over a gap.
        public float GroundTopAt(float x)
        {
            foreach (var g in Ground)
                if (g.Start <= x && x <= g.End) return g.Top;
            foreach (var r in Ramps)
                if (r.Start <= x && x <= r.End) return r.FromTop + (r.ToTop - r.FromTop) * (x - r.Start) / (r.End - r.Start);
            return float.NaN;
        }

        // Rise per unit of run at x: 0 on flat ground and over gaps.
        public float SurfaceSlopeAt(float x)
        {
            foreach (var r in Ramps)
                if (r.Start < x && x < r.End) return (r.ToTop - r.FromTop) / (r.End - r.Start);
            return 0f;
        }

        public bool IsRampJoint(float end, float start)
        {
            foreach (var r in Ramps)
                if (System.Math.Abs(r.Start - end) < 1e-3f && System.Math.Abs(r.End - start) < 1e-3f) return true;
            return false;
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
        public const float BeeHeight = 0.9f;
        public const float BeeAmplitude = 0.4f;
        private const float BeeRadius = 0.35f;
        private const float PlayerCenterAboveFeet = 0.5f;
        private const float CoinClearance = 1.5f;

        private sealed class Builder
        {
            public readonly LevelPlan Plan = new LevelPlan();
            // Why each platform exists (0 hop, 1 end of a split, 2 middle of a split), for choosing its look.
            public readonly List<int> PlatformRoles = new List<int>();
            // Full jumps over a gap, slime or bee (take-off X, take-off ground top): candidates for a coin arc.
            public readonly List<(float X, int Top)> ArcJumps = new List<(float X, int Top)>();
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
                if (S.NoSpikes) return;
                Plan.Spikes.Add((X, Top));
                Plan.Jumps.Add((X - SpikeLeadSeconds * V, ShortHold));
            }

            public void Gap()
            {
                float width = S.GapSeconds * V;
                Plan.Ground.Add((_groundStart, X, Top));
                Plan.Jumps.Add((X - GapLeadSeconds * V, FullHold));
                ArcJumps.Add((X - GapLeadSeconds * V, Top));
                if (S.WaterChance > 0f && Rng.NextDouble() < S.WaterChance) Plan.Water.Add((X, X + width, Top - 0.6f));
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

            // A gentle slope of 2 units per tile, up or down; the flat stretch before it ends where it starts.
            public void Ramp(int tiles, bool up)
            {
                float length = 2f * tiles;
                int to = up ? Top + tiles : Top - tiles;
                Plan.Ground.Add((_groundStart, X, Top));
                Plan.Ramps.Add((X, X + length, Top, to));
                X += length;
                _groundStart = X;
                Top = to;
            }

            // A slime pacing 1.5 units on flat ground. One full jump stays above it for ~0.65 s, which clears the whole
            // patrol from this take-off at run speeds of 5 and up.
            public void Walker()
            {
                Plan.Walkers.Add((X, X + WalkerPatrol, Top));
                Plan.Jumps.Add((X - 0.85f - 0.05f * V, FullHold));
                ArcJumps.Add((X - 0.85f - 0.05f * V, Top));
            }

            public void Saw()
            {
                Plan.Saws.Add((X, Top));
                Plan.Jumps.Add((X - SpikeLeadSeconds * V, FullHold));
            }

            // A bee at head height: the full jump leaves early enough that the feet are above the bee's highest point (plus
            // a little room) by the time the runner's front edge reaches it.
            public void Bee()
            {
                float clear = BeeHeight + BeeAmplitude + BeeRadius + StepClearance * 0.5f;
                // + 0.08 s like a step: the press can land a frame late and the jump starts on the next physics step.
                float lead = (JumpReach.RiseTime(S, clear) + 0.08f) * V + HalfPlayerWidth + BeeRadius;
                Plan.Bees.Add((X, Top + BeeHeight, BeeAmplitude));
                Plan.Jumps.Add((X - lead, FullHold));
                ArcJumps.Add((X - lead, Top));
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
                PlatformRoles.Add(0);
                Spike();
            }

            // High route: three platforms (2, 3, 2 tiles up); low route: ground with a spike under the first and last.
            public void Split()
            {
                float x0 = X;
                Plan.Platforms.Add((x0, x0 + PlatformWidth, Top + 2));
                Plan.Platforms.Add((x0 + 4.5f, x0 + 4.5f + PlatformWidth, Top + 3));
                Plan.Platforms.Add((x0 + 9f, x0 + 9f + PlatformWidth, Top + 2));
                PlatformRoles.Add(1);
                PlatformRoles.Add(2);
                PlatformRoles.Add(1);
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
                bool ramp = Rng.NextDouble() < S.RampChance;
                if (canUp && (!canDown || Rng.NextDouble() < 0.5))
                {
                    if (ramp) Ramp(tiles, true);
                    else StepUp(tiles);
                }
                else if (canDown)
                {
                    if (ramp) Ramp(tiles, false);
                    else StepDown(tiles);
                }
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
            StylePlatforms(b.Plan, b.PlatformRoles, s.Seed);
            Decorate(b.Plan, s);
            PlaceCoins(b, s);
            PlaceBackdrop(b.Plan, s);
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
            if (s.BeeChance > 0f && b.Rng.NextDouble() < s.BeeChance)
            {
                b.Bee();
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

        // Hops become "!" block rows or plain ledges; a split's outer ledges stand on pillars, its middle one is a bridge.
        // Own random stream, so looks never change the layout.
        private static void StylePlatforms(LevelPlan plan, List<int> roles, int seed)
        {
            var rng = new System.Random(seed * 17 + 3);
            foreach (int role in roles)
            {
                double roll = rng.NextDouble();
                plan.PlatformStyles.Add(role == 2 ? PlatformStyle.Bridge
                    : role == 1 ? (roll < 0.7 ? PlatformStyle.PillarLedge : PlatformStyle.Ledge)
                    : (roll < 0.6 ? PlatformStyle.Blocks : PlatformStyle.Ledge));
            }
        }

        // Scatters decorations on the ground tops with its own random stream (so terrain never changes), keeping them
        // 1.5 units clear of every hazard, jump take-off and gap edge, and off segment edges.
        private static void Decorate(LevelPlan plan, LevelPlanSettings s)
        {
            if (s.DecorationDensity <= 0f || s.DecorationWeights == null || s.DecorationWeights.Length == 0) return;
            const float Clear = 1.5f, EdgeInset = 0.6f, Spacing = 1f;
            var rng = new System.Random(s.Seed * 31 + 7);

            var blocked = new List<float>();
            foreach (var p in plan.Spikes) blocked.Add(p.X);
            foreach (var p in plan.Saws) blocked.Add(p.X);
            foreach (var p in plan.Bees) blocked.Add(p.X);
            foreach (var j in plan.Jumps) blocked.Add(j.X);
            for (int i = 0; i + 1 < plan.Ground.Count; i++)
            {
                if (plan.Ground[i + 1].Start - plan.Ground[i].End <= 0.01f) continue;
                if (plan.IsRampJoint(plan.Ground[i].End, plan.Ground[i + 1].Start)) continue;
                blocked.Add(plan.Ground[i].End);
                blocked.Add(plan.Ground[i + 1].Start);
            }

            float total = 0f;
            foreach (var g in plan.Ground) total += System.Math.Max(0f, g.End - g.Start - 2f * EdgeInset);
            int target = (int)System.Math.Round(s.DecorationDensity * TotalGround(plan) / 10f);
            float weightSum = 0f;
            foreach (float w in s.DecorationWeights) weightSum += w;

            for (int attempt = 0; attempt < target * 40 && plan.Decorations.Count < target && total > 0f; attempt++)
            {
                // Pick a spot uniformly along the usable ground.
                double pick = rng.NextDouble() * total;
                float x = float.NaN, top = 0f;
                foreach (var g in plan.Ground)
                {
                    float usable = System.Math.Max(0f, g.End - g.Start - 2f * EdgeInset);
                    if (pick < usable)
                    {
                        x = g.Start + EdgeInset + (float)pick;
                        top = g.Top;
                        break;
                    }
                    pick -= usable;
                }
                if (float.IsNaN(x) || !IsClear(plan, blocked, x, Clear, Spacing)) continue;

                double k = rng.NextDouble() * weightSum;
                int kind = 0;
                while (kind < s.DecorationWeights.Length - 1 && k >= s.DecorationWeights[kind]) k -= s.DecorationWeights[kind++];
                plan.Decorations.Add((x, top, kind));
            }
            plan.Decorations.Sort((a, c) => a.X.CompareTo(c.X));
        }

        // Coins with their own random stream (so terrain never changes): an arc of three (rising, apex, falling) along
        // some full jumps, at the runner's centre height, and a row one unit above each high-route ledge. Any coin near a
        // hazard, inside the ground or inside a ledge is dropped, so a coin never lures the player into a hit.
        private static void PlaceCoins(Builder b, LevelPlanSettings s)
        {
            var plan = b.Plan;
            var rng = new System.Random(s.Seed * 13 + 5);
            var candidates = new List<(float X, float Y)>();

            double g = System.Math.Abs(s.Gravity) * s.GravityScale;
            double v0 = System.Math.Sqrt(2.0 * g * s.JumpHeight);
            double apex = v0 / g;
            double fall = System.Math.Sqrt(2.0 * 0.4 * s.JumpHeight / (g * s.FallMultiplier));
            foreach (var (x, top) in b.ArcJumps)
            {
                if (s.CoinChance <= 0f || rng.NextDouble() >= s.CoinChance) continue;
                foreach (double t in new[] { apex * 0.5, apex, apex + fall })
                {
                    double height = t <= apex ? v0 * t - 0.5 * g * t * t : s.JumpHeight - 0.5 * g * s.FallMultiplier * (t - apex) * (t - apex);
                    candidates.Add((x + (float)(t * s.RunSpeed), top + (float)height + PlayerCenterAboveFeet));
                }
            }
            for (int i = 0; i < plan.Platforms.Count; i++)
            {
                if (b.PlatformRoles[i] == 0) continue;
                var (left, right, top) = plan.Platforms[i];
                for (float x = left + 0.5f; x < right; x += 1f) candidates.Add((x, top + 1f));
            }

            foreach (var c in candidates)
                if (CoinIsSafe(plan, c.X, c.Y)) plan.Coins.Add(c);
        }

        // A backdrop prop every 6–12 units along each flat ground top (never on ramps), with its own random stream, kept
        // 1.5 units clear of spikes, saws, bees and slime patrols so it never sits behind a hazard.
        private static void PlaceBackdrop(LevelPlan plan, LevelPlanSettings s)
        {
            var weights = s.BackdropWeights;
            if (weights == null || weights.Length == 0) return;
            const float Inset = 1f, Clear = 1.5f, MinGap = 6f, MaxGap = 12f;
            var rng = new System.Random(s.Seed * 7 + 11);
            float weightSum = 0f;
            foreach (float w in weights) weightSum += w;

            var hazards = new List<float>();
            foreach (var p in plan.Spikes) hazards.Add(p.X);
            foreach (var p in plan.Saws) hazards.Add(p.X);
            foreach (var p in plan.Bees) hazards.Add(p.X);

            float last = float.NegativeInfinity;
            foreach (var g in plan.Ground)
            {
                float x = System.Math.Max(g.Start + Inset, last + MinGap) + (float)rng.NextDouble() * (MaxGap - MinGap) * 0.5f;
                while (x <= g.End - Inset)
                {
                    bool clear = true;
                    foreach (float h in hazards) if (System.Math.Abs(x - h) < Clear) clear = false;
                    foreach (var w in plan.Walkers) if (x >= w.Left - Clear && x <= w.Right + Clear) clear = false;
                    if (clear)
                    {
                        double k = rng.NextDouble() * weightSum;
                        int kind = 0;
                        while (kind < weights.Length - 1 && k >= weights[kind]) k -= weights[kind++];
                        plan.Backdrop.Add((x, g.Top, kind));
                        last = x;
                        x += MinGap + (float)rng.NextDouble() * (MaxGap - MinGap);
                    }
                    else x += 0.5f;
                }
            }
        }

        private static bool CoinIsSafe(LevelPlan plan, float x, float y)
        {
            float ground = plan.GroundTopAt(x);
            if (!float.IsNaN(ground) && y - ground < 0.5f) return false;
            foreach (var p in plan.Spikes) if (Distance(x, y, p.X, p.Y + 0.4f) < CoinClearance) return false;
            foreach (var p in plan.Saws) if (Distance(x, y, p.X, p.Y + 0.4f) < CoinClearance) return false;
            foreach (var p in plan.Bees) if (Distance(x, y, p.X, p.BaseY) < CoinClearance + p.Amplitude) return false;
            foreach (var w in plan.Walkers)
            {
                float dx = System.Math.Max(0f, System.Math.Max(w.Left - x, x - w.Right));
                float dy = System.Math.Max(0f, y - (w.Y + 1f));
                if (System.Math.Sqrt(dx * dx + dy * dy) < CoinClearance) return false;
            }
            foreach (var p in plan.Platforms)
                if (x > p.Left - 0.5f && x < p.Right + 0.5f && y > p.Top - 1.5f && y < p.Top + 0.5f) return false;
            return true;
        }

        private static double Distance(float x0, float y0, float x1, float y1) =>
            System.Math.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1));

        private static float TotalGround(LevelPlan plan)
        {
            float sum = 0f;
            foreach (var g in plan.Ground) sum += g.End - g.Start;
            return sum;
        }

        private static bool IsClear(LevelPlan plan, List<float> blocked, float x, float clear, float spacing)
        {
            foreach (float b in blocked)
                if (System.Math.Abs(x - b) < clear) return false;
            foreach (var w in plan.Walkers)
                if (x >= w.Left - clear && x <= w.Right + clear) return false;
            foreach (var d in plan.Decorations)
                if (System.Math.Abs(x - d.X) < spacing) return false;
            return true;
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
