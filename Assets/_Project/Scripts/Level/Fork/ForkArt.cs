using UnityEngine;

namespace MathRunner.Level
{
    // The sprites a fork in the path is drawn with (Kenney New Platformer Pack): route signs, the coin block used as the
    // treasure chest (shut / opened), the coin it pops, and the dirt strip tinted into mud.
    [CreateAssetMenu(menuName = "Math Runner/Fork Art", fileName = "ForkArt")]
    public class ForkArt : ScriptableObject
    {
        [SerializeField] private Sprite _sign;
        [SerializeField] private Sprite _chestClosed;
        [SerializeField] private Sprite _chestOpen;
        [SerializeField] private Sprite _coin;
        [SerializeField] private Sprite _mud;
        [SerializeField] private Color _mudTint = new Color(0.55f, 0.36f, 0.2f, 1f);
        [SerializeField, Min(0)] private int _chestCoins = 5;

        public Sprite Sign => _sign;
        public Sprite ChestClosed => _chestClosed;
        public Sprite ChestOpen => _chestOpen;
        public Sprite Coin => _coin;
        public Sprite Mud => _mud;
        public Color MudTint => _mudTint;
        public int ChestCoins => _chestCoins;

        // Editor setup.
        public void SetSprites(Sprite sign, Sprite chestClosed, Sprite chestOpen, Sprite coin, Sprite mud)
        {
            _sign = sign;
            _chestClosed = chestClosed;
            _chestOpen = chestOpen;
            _coin = coin;
            _mud = mud;
        }
    }
}
