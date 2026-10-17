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
        // Seconds of a Question Zone after its decision line: landing air time + platform span.
        public float ZoneSpanSeconds;
        public int HazardsBetweenQuestions;
        public float GapChance;
        public int Seed;

        // Spacing, in seconds of running (distance = seconds × run speed).
        public float IntroSeconds;
        public float HazardSpacingSeconds;
        public float GapSeconds;
        public float AfterZoneSeconds;
        public float OutroSeconds;

        public static LevelPlanSettings From(LevelConfig config, float zoneSpanSeconds) => new LevelPlanSettings
        {
            QuestionCount = config.QuestionCount,
            RunSpeed = config.RunSpeed,
            TimedRatio = config.TimedRatio,
            TimeLimit = config.TimeLimit,
            UntimedLeadSeconds = config.UntimedLeadSeconds,
            ZoneSpanSeconds = zoneSpanSeconds,
            HazardsBetweenQuestions = config.HazardsBetweenQuestions,
            GapChance = config.GapChance,
            Seed = config.LayoutSeed,
            IntroSeconds = 2f,
            HazardSpacingSeconds = 1.6f,
            GapSeconds = 0.4f,
            AfterZoneSeconds = 3f,
            OutroSeconds = 2f,
        };
    }

    public sealed class LevelPlan
    {
        public readonly List<(float Start, float End)> Ground = new List<(float Start, float End)>();
        public readonly List<float> Spikes = new List<float>();
        public readonly List<(float X, bool Timed, float Lead)> Zones = new List<(float X, bool Timed, float Lead)>();
        public readonly List<float> Checkpoints = new List<float>();
        public float FinishX;
        public float EndX;

        public float ZoneLength(int index, LevelPlanSettings s) => (Zones[index].Lead + s.ZoneSpanSeconds) * s.RunSpeed;
    }

    // Lays out a level as: intro, then per question [hazards → Question Zone → recovery run], then the finish.
    // Pure and deterministic for a given seed, so layouts can be checked by EditMode tests.
    public static class LevelPlanner
    {
        public const float StartX = -10f;

        public static LevelPlan Create(LevelPlanSettings s)
        {
            var rng = new System.Random(s.Seed);
            var plan = new LevelPlan();
            float v = s.RunSpeed;
            float groundStart = StartX;
            float x = s.IntroSeconds * v;

            for (int q = 0; q < s.QuestionCount; q++)
            {
                for (int h = 0; h < s.HazardsBetweenQuestions; h++)
                {
                    x += s.HazardSpacingSeconds * v;
                    if (rng.NextDouble() < s.GapChance)
                    {
                        float width = s.GapSeconds * v;
                        plan.Ground.Add((groundStart, x));
                        groundStart = x + width;
                        x += width;
                    }
                    else
                    {
                        plan.Spikes.Add(x);
                    }
                }
                x += s.HazardSpacingSeconds * v;

                bool timed = s.TimeLimit > 0f && IsTimed(q, s.TimedRatio);
                float lead = timed ? s.TimeLimit : System.Math.Max(s.UntimedLeadSeconds, s.TimeLimit);
                plan.Zones.Add((x, timed, lead));
                x += (lead + s.ZoneSpanSeconds) * v;
                plan.Checkpoints.Add(x + 1f);
                x += s.AfterZoneSeconds * v;
            }

            x += s.OutroSeconds * v;
            plan.FinishX = x;
            plan.EndX = x + 3f * v;
            plan.Ground.Add((groundStart, plan.EndX));
            return plan;
        }

        // Spreads timed questions evenly: with ratio 0.5 every second question is timed, starting with the second.
        public static bool IsTimed(int questionIndex, float ratio) =>
            System.Math.Floor((questionIndex + 1) * ratio + 1e-4) > System.Math.Floor(questionIndex * ratio + 1e-4);
    }
}
