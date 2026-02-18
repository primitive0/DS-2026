using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string text)
    {
        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();
        var db = _redis.GetDatabase();

        await db.StringSetAsync($"TEXT-{id}", text);

        double rank = CalculateRank(text);
        await db.StringSetAsync($"RANK-{id}", rank.ToString(System.Globalization.CultureInfo.InvariantCulture));

        double similarity = await CalculateSimilarityAsync(db, text);
        await db.StringSetAsync($"SIMILARITY-{id}", similarity.ToString(System.Globalization.CultureInfo.InvariantCulture));

        return Redirect($"summary?id={id}");
    }

    private static double CalculateRank(string text)
    {
        if (text.Length == 0)
            return 0.0;

        int nonAlpha = 0;
        foreach (var ch in text)
        {
            if (!IsLatinOrCyrillicLetter(ch))
                nonAlpha++;
        }

        return (double)nonAlpha / text.Length;
    }

    private static bool IsLatinOrCyrillicLetter(char ch)
    {
        if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'))
            return true;

        if ((ch >= 'А' && ch <= 'Я') || (ch >= 'а' && ch <= 'я') || ch == 'Ё' || ch == 'ё')
            return true;

        return false;
    }

    private static async Task<double> CalculateSimilarityAsync(IDatabase db, string text)
    {
        string hash = Sha256Hex(text);
        string seenKey = "SEEN-" + hash;

        bool firstTime = await db.StringSetAsync(seenKey, "1", when: When.NotExists);

        return firstTime ? 0.0 : 1.0;
    }

    private static string Sha256Hex(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}