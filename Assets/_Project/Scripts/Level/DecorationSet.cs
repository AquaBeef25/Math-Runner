using UnityEngine;

namespace MathRunner.Level
{
    // The props a world scatters on its ground (grass, bushes, rocks...), each with a relative chance, plus bigger
    // backdrop props (trees, fences, houses) drawn just behind the play area.
    [CreateAssetMenu(menuName = "Math Runner/Decoration Set", fileName = "Decorations")]
    public class DecorationSet : ScriptableObject
    {
        [SerializeField] private Sprite[] _sprites = new Sprite[0];
        [SerializeField] private float[] _weights = new float[0];
        [Header("Backdrop (just behind the play area)")]
        [SerializeField] private Sprite[] _backdropSprites = new Sprite[0];
        [SerializeField] private float[] _backdropWeights = new float[0];

        public float[] Weights => _weights;
        public float[] BackdropWeights => _backdropSprites.Length == 0 ? null : _backdropWeights;

        public Sprite Sprite(int kind) => _sprites.Length == 0 ? null : _sprites[Mathf.Clamp(kind, 0, _sprites.Length - 1)];

        public Sprite BackdropSprite(int kind) =>
            _backdropSprites.Length == 0 ? null : _backdropSprites[Mathf.Clamp(kind, 0, _backdropSprites.Length - 1)];
    }
}
