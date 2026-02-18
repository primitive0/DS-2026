using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using System.Globalization;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public bool FoundRecord { get; set; }
    public double Rank { get; set; }
    public double Similarity { get; set; }

    public async Task OnGetAsync(string id)
    {
        _logger.LogDebug(id);

        var db = _redis.GetDatabase();

        var rankVal = await db.StringGetAsync($"RANK-{id}");
        var simVal = await db.StringGetAsync($"SIMILARITY-{id}");

        double? rank = TryParseDouble(rankVal);
        double? similarity = TryParseDouble(simVal);

        if (rank.HasValue && similarity.HasValue) {
            FoundRecord = true;
            Rank = rank.Value;
            Similarity = similarity.Value;
        } else {
            FoundRecord = false;
            Rank = 0.0;
            Similarity = 0.0;
        }
    }

    private static double? TryParseDouble(RedisValue v)
    {
        if (!v.HasValue)
            return null;

        if (double.TryParse(v.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return d;

        return null;
    }
}
