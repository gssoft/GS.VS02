#

## 1

### Потокобезопасность в Trading.Api.Orders

Сейчас там:

```csharp
var orders = new List<Order>();
```

Для демо нормально, но это не потокобезопасно. Если два запроса одновременно добавят ордер, могут быть проблемы.

Для начала можно заменить на:

```csharp
using System.Collections.Concurrent;

var orders = new ConcurrentDictionary<Guid, Order>();
```

И эндпоинты:

```csharp
app.MapGet("/api/orders", () => 
    orders.Values.OrderBy(x => x.CreatedAt).ToList());

app.MapPost("/api/orders", (Order order) =>
{
    order.Id = Guid.NewGuid();
    order.CreatedAt = DateTime.UtcNow;

    orders[order.Id] = order;

    return Results.Created(\$"/api/orders/{order.Id}", order);
});
```
