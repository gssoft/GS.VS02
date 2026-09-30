using MassTransit;
using MassTransit.Transports;
using Trading.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// В Program.cs, после builder.AddServiceDefaults();

// Настраиваем MassTransit для подключения к RabbitMQ.
// "rabbit" — это имя ресурса, которое мы задали в AppHost.
builder.AddMassTransitRabbitMq("rabbit", x =>
{
    // Здесь можно зарегистрировать потребителей (consumers), но Orders API их не имеет.
    // x.AddConsumer<SomeConsumer>(); 
});

var app = builder.Build();
app.MapDefaultEndpoints();


// Простой in-memory список ордеров (для примера)
var orders = new List<Order>();

app.MapGet("/api/orders", () => Results.Ok(orders));

//app.MapPost("/api/orders", (Order order) =>
//{
//    order.Id = Guid.NewGuid();
//    order.CreatedAt = DateTime.UtcNow;
//    orders.Add(order);
//    return Results.Created($"/api/orders/{order.Id}", order);
//});

// Ваш обработчик POST /api/orders
app.MapPost("/api/orders", async (Order order, IPublishEndpoint publishEndpoint) =>
{
    order.Id = Guid.NewGuid();
order.CreatedAt = DateTime.UtcNow;
orders.Add(order);

// Публикуем событие в RabbitMQ
await publishEndpoint.Publish(new OrderPlaced(
    order.Id,
    order.Symbol,
    order.Price,
    order.Quantity,
    order.Side,
    order.CreatedAt));

return Results.Created($"/api/orders/{order.Id}", order);
});

app.Run();

record Order
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Side { get; set; } = "Buy"; // Buy / Sell
    public DateTime CreatedAt { get; set; }
}

