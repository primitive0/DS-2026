using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using TextMessaging;

namespace Valuator.Pages;

public class IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis, RabbitPublisher publisher) : PageModel
{
    [BindProperty]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Text { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var text = Text ?? "";
        var id = Guid.NewGuid();
        try
        {
            var db = redis.GetDatabase();
            await db.StringSetAsync(StorageKeys.Text(id), text);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            var firstTime = await db.StringSetAsync($"SEEN-{hash.ToLowerInvariant()}", "1", when: When.NotExists);
            var similarity = firstTime ? 0.0 : 1.0;
            await db.StringSetAsync(StorageKeys.Similarity(id), similarity.ToString(CultureInfo.InvariantCulture));
            await publisher.PublishEventAsync(new SimilarityCalculated(id, similarity), HttpContext.RequestAborted);
            await publisher.PublishRankTaskAsync(id, HttpContext.RequestAborted);
            logger.LogInformation("Accepted TextId={TextId} Similarity={Similarity} Instance={Instance}",
                id, similarity, Environment.MachineName);
            return Redirect($"/summary?id={id}");
        }
        catch (Exception ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to submit text {TextId}", id);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ModelState.AddModelError("", "Не удалось отправить текст на оценку. Попробуйте ещё раз позже.");
            return Page();
        }
    }
}
