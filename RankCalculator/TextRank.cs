namespace RankCalculator;

public static class TextRank
{
    public static double Calculate(string text)
    {
        if (text.Length == 0) return 0.0;
        var nonAlpha = 0;
        foreach (var ch in text)
        {
            if (!IsLatinOrCyrillicLetter(ch)) nonAlpha++;
        }
        return (double)nonAlpha / text.Length;
    }

    private static bool IsLatinOrCyrillicLetter(char ch) =>
        ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= 'А' and <= 'Я' or >= 'а' and <= 'я' or 'Ё' or 'ё';
}
