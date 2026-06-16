using Microsoft.AspNetCore.Mvc.RazorPages;
using QuotesClientRazor01.Models;
using QuotesClientRazor01.Services;

namespace QuotesClientRazor01.Pages;

public class IndexModel : PageModel
{
    private readonly QuoteCache _quoteCache;
    private readonly IConfiguration _configuration;

    public List<StockQuote> Quotes { get; set; } = new();
    public string ChannelName { get; set; } = "finance";
    public string ChannelDisplayName { get; set; } = "FINANCE STOCKS";
    public string ChannelColor { get; set; } = "#17a2b8"; // Cyan

    public IndexModel(QuoteCache quoteCache, IConfiguration configuration)
    {
        _quoteCache = quoteCache;
        _configuration = configuration;
    }

    public void OnGet()
    {
        ChannelName = _configuration.GetValue<string>("QuoteChannel", "finance");

        // Настраиваем отображение в зависимости от канала
        switch (ChannelName.ToLower())
        {
            case "tech":
                ChannelDisplayName = "TECH STOCKS";
                ChannelColor = "#28a745"; // Green
                break;
            case "consumer":
                ChannelDisplayName = "CONSUMER STOCKS";
                ChannelColor = "#ffc107"; // Yellow
                break;
            case "finance":
                ChannelDisplayName = "FINANCE STOCKS";
                ChannelColor = "#17a2b8"; // Cyan
                break;
            case "energy":
                ChannelDisplayName = "ENERGY STOCKS";
                ChannelColor = "#6f42c1"; // Magenta/Purple
                break;
        }

        Quotes = _quoteCache.LatestQuotes.Values.ToList();
    }
}

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;

//namespace QuotesClientRazor01.Pages
//{
//    public class IndexModel : PageModel
//    {
//        public void OnGet()
//        {

//        }
//    }
//}
