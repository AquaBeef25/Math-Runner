using UnityEngine;

namespace MathRunner.Level
{
    // A world's harmless moving life: how often each kind appears and the sprites it is drawn with. A kind with no
    // sprites never appears, whatever its weight.
    [CreateAssetMenu(menuName = "Math Runner/Life Set", fileName = "Life")]
    public class LifeSet : ScriptableObject
    {
        // One look of a kind: its animation frames (walker a/b, hopper idle/jump, swing chain/crate...).
        [System.Serializable]
        public class Variant
        {
            [SerializeField] private Sprite[] _frames;

            public Variant(params Sprite[] frames) => _frames = frames;

            public Sprite[] Frames => _frames;
        }

        [System.Serializable]
        private class Kind
        {
            public float Weight;
            public Variant[] Variants = new Variant[0];
        }

        [SerializeField] private Kind[] _kinds = NewKinds();
        [SerializeField, Range(0f, 1f)] private float _fishChance;
        [SerializeField] private Variant[] _fish = new Variant[0];
        [SerializeField] private Sprite[] _emotes = new Sprite[0];

        private static Kind[] NewKinds()
        {
            var kinds = new Kind[System.Enum.GetValues(typeof(LifeKind)).Length];
            for (int i = 0; i < kinds.Length; i++) kinds[i] = new Kind();
            return kinds;
        }

        // Relative chance per LifeKind (index = (int)kind); 0 for kinds without sprites.
        public float[] Weights
        {
            get
            {
                var w = new float[_kinds.Length];
                for (int i = 0; i < w.Length; i++) w[i] = _kinds[i].Variants.Length > 0 ? _kinds[i].Weight : 0f;
                return w;
            }
        }

        public int[] Variants
        {
            get
            {
                var v = new int[_kinds.Length];
                for (int i = 0; i < v.Length; i++) v[i] = Mathf.Max(1, _kinds[i].Variants.Length);
                return v;
            }
        }

        public float FishChance => _fish.Length > 0 ? _fishChance : 0f;
        public Sprite[] Emotes => _emotes;

        public Sprite[] Frames(LifeKind kind, int variant)
        {
            var variants = _kinds[(int)kind].Variants;
            return variants.Length == 0 ? new Sprite[0] : variants[Mathf.Clamp(variant, 0, variants.Length - 1)].Frames;
        }

        public Sprite[] FishFrames(int variant) =>
            _fish.Length == 0 ? new Sprite[0] : _fish[Mathf.Clamp(variant, 0, _fish.Length - 1)].Frames;

        public int FishVariants => _fish.Length;

        // Editor setup and tests.
        public void SetKind(LifeKind kind, float weight, params Variant[] variants)
        {
            if (_kinds == null || _kinds.Length != System.Enum.GetValues(typeof(LifeKind)).Length) _kinds = NewKinds();
            _kinds[(int)kind] = new Kind { Weight = weight, Variants = variants ?? new Variant[0] };
        }

        public void SetFish(float chance, params Variant[] variants)
        {
            _fishChance = chance;
            _fish = variants ?? new Variant[0];
        }

        public void SetEmotes(params Sprite[] emotes) => _emotes = emotes ?? new Sprite[0];
    }
}
