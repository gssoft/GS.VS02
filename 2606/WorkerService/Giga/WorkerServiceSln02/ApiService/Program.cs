var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IMessageQueue, InMemoryQueue>();
var app = builder.Build();

app.MapPost("/enqueue", (string task, IMessageQueue queue) =>
{
    queue.Enqueue(task);
    return Results.Accepted($"Задача '{task}' принята");
});

app.MapGet("/", () => "API работает. Отправляйте POST на /enqueue");
app.Run();

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
