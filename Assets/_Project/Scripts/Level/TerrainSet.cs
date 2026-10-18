using UnityEngine;

namespace MathRunner.Level
{
    // The tiles a world's ground is drawn with (Kenney grass, sand...): ground blocks, ramps, floating ledges and the
    // pillars under them. Art only; colliders never depend on it. A level without one keeps the prefabs' own tiles.
    [CreateAssetMenu(menuName = "Math Runner/Terrain Set", fileName = "Terrain")]
    public class TerrainSet : ScriptableObject
    {
        [Header("Ground: grass/top row")]
        [SerializeField] private Sprite _topLeft;      // terrain_<x>_block_top_left
        [SerializeField] private Sprite _top;          // terrain_<x>_block_top
        [SerializeField] private Sprite _topRight;     // terrain_<x>_block_top_right
        [SerializeField] private Sprite _topSingle;    // terrain_<x>_vertical_top
        [Header("Ground: fill below")]
        [SerializeField] private Sprite _fillLeft;     // terrain_<x>_block_left
        [SerializeField] private Sprite _fill;         // terrain_<x>_block_center
        [SerializeField] private Sprite _fillRight;    // terrain_<x>_block_right
        [SerializeField] private Sprite _fillSingle;   // terrain_<x>_vertical_middle
        [Header("Ramps")]
        [SerializeField] private Sprite _rampHigh;     // terrain_<x>_ramp_long_a
        [SerializeField] private Sprite _rampLow;      // terrain_<x>_ramp_long_b
        [SerializeField] private Sprite _rampUnder;    // terrain_<x>_ramp_long_c
        [Header("Floating ledges")]
        [SerializeField] private Sprite _ledgeLeft;    // terrain_<x>_horizontal_left
        [SerializeField] private Sprite _ledgeMiddle;  // terrain_<x>_horizontal_middle
        [SerializeField] private Sprite _ledgeRight;   // terrain_<x>_horizontal_right
        [SerializeField] private Sprite _pillar;       // terrain_<x>_vertical_middle

        public Sprite TopLeft => _topLeft;
        public Sprite Top => _top;
        public Sprite TopRight => _topRight;
        public Sprite TopSingle => _topSingle;
        public Sprite FillLeft => _fillLeft;
        public Sprite Fill => _fill;
        public Sprite FillRight => _fillRight;
        public Sprite FillSingle => _fillSingle;
        public Sprite RampHigh => _rampHigh;
        public Sprite RampLow => _rampLow;
        public Sprite RampUnder => _rampUnder;
        public Sprite LedgeLeft => _ledgeLeft;
        public Sprite LedgeMiddle => _ledgeMiddle;
        public Sprite LedgeRight => _ledgeRight;
        public Sprite Pillar => _pillar;

        // Editor setup and tests.
        public void SetGround(Sprite topLeft, Sprite top, Sprite topRight, Sprite topSingle,
            Sprite fillLeft, Sprite fill, Sprite fillRight, Sprite fillSingle)
        {
            _topLeft = topLeft;
            _top = top;
            _topRight = topRight;
            _topSingle = topSingle;
            _fillLeft = fillLeft;
            _fill = fill;
            _fillRight = fillRight;
            _fillSingle = fillSingle;
        }

        public void SetRampsAndLedges(Sprite rampHigh, Sprite rampLow, Sprite rampUnder,
            Sprite ledgeLeft, Sprite ledgeMiddle, Sprite ledgeRight, Sprite pillar)
        {
            _rampHigh = rampHigh;
            _rampLow = rampLow;
            _rampUnder = rampUnder;
            _ledgeLeft = ledgeLeft;
            _ledgeMiddle = ledgeMiddle;
            _ledgeRight = ledgeRight;
            _pillar = pillar;
        }
    }
}
