using UnityEngine;

namespace MathRunner.Level
{
    // The sprites a fork in the path is drawn with (Kenney New Platformer Pack): route signs, the coin block used as the
    // treasure chest (shut / opened), the coin it pops, and the spikes a wrong route raises.
    [CreateAssetMenu(menuName = "Math Runner/Fork Art", fileName = "ForkArt")]
    public class ForkArt : ScriptableObject
    {
        [SerializeField] private Sprite _sign;
        [SerializeField] private Sprite _chestClosed;
        [SerializeField] private Sprite _chestOpen;
        [SerializeField] private Sprite _coin;
        [SerializeField] private Sprite _spikes;
        [SerializeField, Min(0)] private int _chestCoins = 5;

        public Sprite Sign => _sign;
        public Sprite ChestClosed => _chestClosed;
        public Sprite ChestOpen => _chestOpen;
        public Sprite Coin => _coin;
        public Sprite Spikes => _spikes;
        public int ChestCoins => _chestCoins;

        // Editor setup.
        public void SetSprites(Sprite sign, Sprite chestClosed, Sprite chestOpen, Sprite coin, Sprite spikes)
        {
            _sign = sign;
            _chestClosed = chestClosed;
            _chestOpen = chestOpen;
            _coin = coin;
            _spikes = spikes;
        }
    }
}
