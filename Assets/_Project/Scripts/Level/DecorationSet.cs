using UnityEngine;

namespace MathRunner.Level
{
    // The props a world scatters on its ground (grass, bushes, rocks...), each with a relative chance.
    [CreateAssetMenu(menuName = "Math Runner/Decoration Set", fileName = "Decorations")]
    public class DecorationSet : ScriptableObject
    {
        [SerializeField] private Sprite[] _sprites = new Sprite[0];
        [SerializeField] private float[] _weights = new float[0];

        public float[] Weights => _weights;

        public Sprite Sprite(int kind) => _sprites.Length == 0 ? null : _sprites[Mathf.Clamp(kind, 0, _sprites.Length - 1)];
    }
}
