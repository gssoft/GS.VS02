var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultEndpoints();

// Простой in-memory список ордеров (для примера)
var orders = new List<Order>();

app.MapGet("/api/orders", () => Results.Ok(orders));

app.MapPost("/api/orders", (Order order) =>
{
    order.Id = Guid.NewGuid();
    order.CreatedAt = DateTime.UtcNow;
    orders.Add(order);
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

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

//var app = builder.Build();

//app.MapDefaultEndpoints();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
