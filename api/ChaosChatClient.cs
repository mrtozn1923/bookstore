using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Bookstore.Assistant.Api;

// Yalnız Development: X-Chaos header'ı ile gerçek modelin önüne hata enjekte eder
// "fail-after-5": 5 metin parçasından sonra exception, "stall-40": ilk parçadan önce 40 sn bekle
public sealed class ChaosChatClient(IChatClient inner, IHttpContextAccessor http) : DelegatingChatClient(inner)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string chaos = http.HttpContext?.Request.Headers["X-Chaos"].ToString() ?? "";
        int failAfter = chaos.StartsWith("fail-after-") ? int.Parse(chaos["fail-after-".Length..]) : -1;
        if (chaos.StartsWith("stall-"))
        {
            await Task.Delay(TimeSpan.FromSeconds(int.Parse(chaos["stall-".Length..])), cancellationToken);
        }

        int texts = 0;
        await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (failAfter >= 0 && update.Contents.OfType<TextContent>().Any(t => t.Text.Length > 0) && texts++ == failAfter)
            {
                throw new HttpRequestException("Chaos: model connection reset");
            }
            yield return update;
        }
    }
}
