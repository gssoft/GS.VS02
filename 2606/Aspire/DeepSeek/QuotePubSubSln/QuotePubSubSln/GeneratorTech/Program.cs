using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuoteModels;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

var rabbitConnectionString = builder.Configuration.GetConnectionString("rabbitmq")
                             ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        Uri = new Uri(rabbitConnectionString),
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true
    };
    return factory.CreateConnection();
});

builder.Services.AddHostedService<QuoteGeneratorWorker>();

var host = builder.Build();
await host.RunAsync();

// Worker
class QuoteGeneratorWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string[] _tickers = { "GOOGL", "MSFT", "NVDA" };
    private readonly Random _rnd = new();

    public QuoteGeneratorWorker(IConnection connection)
    {
        _connection = connection;
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare("quotes", ExchangeType.Topic, durable: true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var ticker = _tickers[_rnd.Next(_tickers.Length)];
            var quote = new StockQuote(
                ticker,
                Bid: Math.Round((decimal)(_rnd.NextDouble() * 2000), 2),
                Ask: Math.Round((decimal)(_rnd.NextDouble() * 2000), 2),
                Last: Math.Round((decimal)(_rnd.NextDouble() * 2000), 2),
                Volume: _rnd.Next(1, 10000));

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(quote));
            _channel.BasicPublish("quotes", ticker, null, body);

            Console.WriteLine($"[Tech] Published {ticker} @ {quote.Last:C}");
            await Task.Delay(_rnd.Next(500, 1500), stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}