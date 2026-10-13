using RankCalculator;

(string Text, double Expected)[] cases =
[
    ("", 0), ("Hello", 0), ("ПриветЁё", 0), ("AЯzяЁё", 0),
    ("123!?", 1), (" ", 1), ("abc!", 0.25), ("А б", 1.0 / 3),
    ("a\nб\t", 0.5), ("é", 1), ("😀", 1), ("Hello, мир!", 3.0 / 11),
    ("@A[Z`a{z", 0.5), ("ЏАаёѐ", 0.4)
];
foreach (var (text, expected) in cases)
{
    var actual = TextRank.Calculate(text);
    if (Math.Abs(actual - expected) > 1e-12)
        throw new Exception($"Rank for {text}: expected {expected}, got {actual}");
    if (TextRank.Calculate(text) != actual)
        throw new Exception("Repeated calculation changed the result");
}
Console.WriteLine($"Passed {cases.Length} rank cases, including empty input, alphabets, boundaries and repeated calculation.");
