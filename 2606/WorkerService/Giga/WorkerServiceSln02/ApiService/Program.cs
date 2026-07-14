using WorkerServiceSln02.Contracts;

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
