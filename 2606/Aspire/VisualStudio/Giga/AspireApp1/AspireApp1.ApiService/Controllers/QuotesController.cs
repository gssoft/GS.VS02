// File: Controllers/QuotesController.cs
using Microsoft.AspNetCore.Mvc;
using Quotes.Shared.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace AspireApp1.ApiService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuotesController(ILogger<QuotesController> logger, ChannelReader<QuoteDto> reader) : ControllerBase
{
    [HttpGet("stream")]
    public async IAsyncEnumerable<QuoteDto> Stream([EnumeratorCancellation] CancellationToken ct)
    {
        while (await reader.WaitToReadAsync(ct))
        {
            while (reader.TryRead(out var quote))
            {
                yield return quote;
            }
        }
    }

    // Опционально: если захотите использовать классический polling вместо SSE
    [HttpGet("latest")]
    public async Task<IReadOnlyCollection<QuoteDto>> GetLatest()
    {
        var list = new List<QuoteDto>();
        while (reader.TryRead(out var quote))
        {
            list.Add(quote);
            if (list.Count >= 20) break; // Отдаем только последние 20
        }
        return list;
    }
}
