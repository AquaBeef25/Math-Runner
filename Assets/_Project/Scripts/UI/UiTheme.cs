using TMPro;
using UnityEngine;

namespace MathRunner.UI
{
    // The chalkboard look in one place: chalk colours, the two fonts and every UI sprite (board, chalk marks, Kenney
    // buttons, hearts, coin, stars). Lives in Resources so UI scripts can load it without wiring.
    [CreateAssetMenu(menuName = "Math Runner/UI Theme", fileName = "UiTheme")]
    public class UiTheme : ScriptableObject
    {
        private const string ResourcePath = "UiTheme";

        [Header("Chalk colours")]
        [SerializeField] private Color _chalkWhite = new Color32(0xF6, 0xF3, 0xE8, 0xFF);
        [SerializeField] private Color _chalkUrgent = new Color32(0xFF, 0x9D, 0x9D, 0xFF);
        [SerializeField] private Color _dimOverlay = new Color(20f / 255f, 30f / 255f, 40f / 255f, 0.45f);
        [Tooltip("Answer colours in key order 1-4.")]
        [SerializeField] private Color[] _answerColors =
        {
            new Color32(0x8F, 0xD0, 0xFF, 0xFF), new Color32(0xA6, 0xEC, 0x9B, 0xFF),
            new Color32(0xFF, 0xE0, 0x8A, 0xFF), new Color32(0xFF, 0x9D, 0x9D, 0xFF)
        };

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset _buttonFont;
        [SerializeField] private TMP_FontAsset _chalkFont;

        [Header("Board")]
        [SerializeField] private Sprite _board;
        [SerializeField] private Sprite _ring;
        [SerializeField] private Sprite _line;
        [SerializeField] private Sprite _tick;
        [SerializeField] private Sprite _cross;

        [Header("HUD")]
        [SerializeField] private Sprite _heartFull;
        [SerializeField] private Sprite _heartEmpty;
        [SerializeField] private Sprite _coin;
        [SerializeField] private Sprite _star;
        [SerializeField] private Sprite _starEmpty;

        [Header("Buttons (index = UiButtonColor)")]
        [SerializeField] private Sprite[] _buttons = new Sprite[5];
        [SerializeField] private Sprite[] _squareButtons = new Sprite[5];
        [SerializeField] private Sprite _roundButton;
        [SerializeField] private Sprite _playIcon;
        [SerializeField] private Sprite _repeatIcon;

        public Color ChalkWhite => _chalkWhite;
        public Color ChalkUrgent => _chalkUrgent;
        public Color DimOverlay => _dimOverlay;
        public TMP_FontAsset ButtonFont => _buttonFont;
        public TMP_FontAsset ChalkFont => _chalkFont;
        public Sprite Board => _board;
        public Sprite Ring => _ring;
        public Sprite Line => _line;
        public Sprite Tick => _tick;
        public Sprite Cross => _cross;
        public Sprite HeartFull => _heartFull;
        public Sprite HeartEmpty => _heartEmpty;
        public Sprite Coin => _coin;
        public Sprite Star => _star;
        public Sprite StarEmpty => _starEmpty;
        public Sprite RoundButton => _roundButton;
        public Sprite PlayIcon => _playIcon;
        public Sprite RepeatIcon => _repeatIcon;

        public Color AnswerColor(int index) => _answerColors[Mathf.Clamp(index, 0, _answerColors.Length - 1)];
        public Sprite Button(UiButtonColor color) => _buttons[(int)color];
        public Sprite SquareButton(UiButtonColor color) => _squareButtons[(int)color];

        public static UiTheme Load() => Resources.Load<UiTheme>(ResourcePath);

        // Editor setup.
        public void SetFonts(TMP_FontAsset buttonFont, TMP_FontAsset chalkFont)
        {
            _buttonFont = buttonFont;
            _chalkFont = chalkFont;
        }

        public void SetBoardSprites(Sprite board, Sprite ring, Sprite line, Sprite tick, Sprite cross)
        {
            _board = board;
            _ring = ring;
            _line = line;
            _tick = tick;
            _cross = cross;
        }

        public void SetHudSprites(Sprite heartFull, Sprite heartEmpty, Sprite coin, Sprite star, Sprite starEmpty)
        {
            _heartFull = heartFull;
            _heartEmpty = heartEmpty;
            _coin = coin;
            _star = star;
            _starEmpty = starEmpty;
        }

        public void SetButtonSprites(Sprite[] buttons, Sprite[] squareButtons, Sprite roundButton, Sprite playIcon, Sprite repeatIcon)
        {
            _buttons = buttons;
            _squareButtons = squareButtons;
            _roundButton = roundButton;
            _playIcon = playIcon;
            _repeatIcon = repeatIcon;
        }
    }
}
