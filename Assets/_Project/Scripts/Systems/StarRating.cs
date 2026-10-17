namespace MathRunner.Systems
{
    // Stars for a finished level: 3 = flawless (every answer right, no life lost), 2 = at least 75% right, 1 = finished.
    public static class StarRating
    {
        public const int Max = 3;

        public static int For(int correct, int answered, int livesLeft, int startingLives)
        {
            if (answered > 0 && correct == answered && livesLeft == startingLives) return 3;
            if (answered > 0 && correct * 4 >= answered * 3) return 2;
            return 1;
        }

        // Plain text until star icons arrive with the art pass: the default font has no ★ glyph.
        public static string Display(int stars) => $"{stars} of {Max} stars";
    }
}
