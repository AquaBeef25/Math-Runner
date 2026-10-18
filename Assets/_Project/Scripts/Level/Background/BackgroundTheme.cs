using UnityEngine;

namespace MathRunner.Level
{
    // The look of a world: the horizon row (hills, trees, ...) and the solid colour that continues below it.
    [CreateAssetMenu(menuName = "Math Runner/Background Theme", fileName = "BackgroundTheme")]
    public class BackgroundTheme : ScriptableObject
    {
        [SerializeField] private Sprite _horizon;
        [SerializeField] private Sprite _fill;

        public Sprite Horizon => _horizon;
        public Sprite Fill => _fill;
    }
}
