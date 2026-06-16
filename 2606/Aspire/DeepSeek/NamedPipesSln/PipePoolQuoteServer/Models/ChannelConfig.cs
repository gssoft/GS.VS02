namespace QuotesServer.Models;

public class ChannelConfig
{
    public string ChannelName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Tickers { get; set; } = new();
    public int MaxClients { get; set; } = 3; // Максимум параллельных клиентов на канал
    public ConsoleColor Color { get; set; } = ConsoleColor.White;
}

