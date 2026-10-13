using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using TextMessaging;

namespace Valuator.Pages;

public class SummaryModel(IConnectionMultiplexer redis) : PageModel
{
    public bool FoundRecord { get; private set; }
    public Guid TextId { get; private set; }
    public double? Rank { get; private set; }
    public double? Similarity { get; private set; }

    public async Task OnGetAsync(string? id)
    {
        if (!Guid.TryParse(id, out var textId)) return;
        TextId = textId;
        var db = redis.GetDatabase();
        var values = await db.StringGetAsync([StorageKeys.Text(textId), StorageKeys.Rank(textId), StorageKeys.Similarity(textId)]);
        FoundRecord = !values[0].IsNull;
        Rank = TryParse(values[1]);
        Similarity = TryParse(values[2]);
    }

    private static double? TryParse(RedisValue value) =>
        double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
}
