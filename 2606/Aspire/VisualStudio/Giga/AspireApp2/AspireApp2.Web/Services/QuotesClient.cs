// File: Services/QuotesClient.cs
using Quotes.Shared.Contracts;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AspireApp1.Web.Services;

public interface IQuotesClient
{
    IAsyncEnumerable<QuoteDto> StreamQuotesAsync(CancellationToken cancellationToken = default);
}

public class QuotesClient(HttpClient httpClient) : IQuotesClient
{
    public async IAsyncEnumerable<QuoteDto> StreamQuotesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/quotes/stream");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!(reader.EndOfStream || cancellationToken.IsCancellationRequested))
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Упрощенный парсер JSON строки (SSE передает чистые JSON объекты строками)
            if (line.StartsWith("data:"))
            {
                var json = line.Substring(5).Trim();
                var quote = JsonSerializer.Deserialize<QuoteDto>(json);
                if (quote != null)
                {
                    yield return quote;
                }
            }
        }
    }
}
