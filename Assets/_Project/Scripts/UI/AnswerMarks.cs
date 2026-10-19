namespace MathRunner.UI
{
    // Which answer circles get a chalk mark after a question: a tick on the right answer, a cross on a wrong pick.
    public static class AnswerMarks
    {
        // chosenIndex -1 = missed. Slots past the question's choices never get a mark.
        public static AnswerMark[] For(int choiceCount, int correctIndex, int chosenIndex, int slots = 4)
        {
            var marks = new AnswerMark[slots];
            if (correctIndex >= 0 && correctIndex < choiceCount && correctIndex < slots) marks[correctIndex] = AnswerMark.Tick;
            if (chosenIndex >= 0 && chosenIndex != correctIndex && chosenIndex < choiceCount && chosenIndex < slots)
                marks[chosenIndex] = AnswerMark.Cross;
            return marks;
        }
    }
}
