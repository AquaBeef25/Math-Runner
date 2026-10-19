namespace MathRunner.Level
{
    // A fork in the path for one question zone. Lanes[0] = high ledge (full jump), [1] = middle ledge (short hop),
    // [2] = low road (no jump) — the same order as the question's choices. BaseTop is the ground height at the split,
    // CommitX where every route has landed (the answer is read there), EndX where the road is back at BaseTop.
    public sealed class ForkPlan
    {
        public const int High = 0, Middle = 1, Low = 2;

        public ForkPlan(int zoneIndex, float splitX, float baseTop, float commitX, float endX, ForkLane[] lanes)
        {
            ZoneIndex = zoneIndex;
            SplitX = splitX;
            BaseTop = baseTop;
            CommitX = commitX;
            EndX = endX;
            Lanes = lanes;
        }

        public int ZoneIndex { get; }
        public float SplitX { get; }
        public float BaseTop { get; }
        public float CommitX { get; }
        public float EndX { get; }
        public ForkLane[] Lanes { get; }
    }
}
