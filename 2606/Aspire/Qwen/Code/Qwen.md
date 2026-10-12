# Start

## 1. Правильная топология в Trading.AppHost/Program.cs

У вас уже есть ядро архитектурного закона:

```csharp
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api");
var marketDataApi = builder.AddProject<Projects.Trading_Api_MarketData>("market-data-api");

var gateway = builder.AddProject<Projects.Trading_Gateway>("gateway")
    .WithReference(ordersApi)
    .WithReference(marketDataApi);

var web = builder.AddProject<Projects.Trading_Web>("web")
    .WithReference(gateway);
    
```

Это уже правильно:

1. web знает только gateway;
2. gateway знает про orders-api и market-data-api;
3. бизнес-сервисы не знают друг о друге;
4. фронтенд не может напрямую обратиться к orders-api или market-data-api, потому что у него нет на них ссылки

---

Это и есть принцип:
Безопасность и архитектурная чистота через физическое отсутствие путей.
Для начала — отлично.

---

## 2. Trading.Gateway уже использует YARP

```text
Trading.Web | v Trading.Gateway | +--> Trading.Api.Orders
```

`Trading.Gateway`

- Пункт 1
- Пункт 2
  - Подпункт (два пробела перед дефисом)
- Пункт 3

---
`Trading.Gateway`

1. Первый шаг
2. Второй шаг
   1. Вложенный нумерованный список
3. Третий шаг

- [ ] Задача выполнена
- [x] Задача в работе

[Текст ссылки](https://example.com)

Для описания архитектуры и псевдокода (чтобы не было никакой подсветки):

```text
Trading.Web | v Trading.Gateway | +--> Trading.Api.Orders
```

Для C# кода: ```csharp public class Gateway { public void Route() { } }```

Для командной строки (Bash): ```bash dotnet run --project Trading.Gateway```

Для JSON: ```json { "symbol": "SBER", "price": 280.50 }```

| Элемент | За что отвечает | Пример |
| :---|:---|:---|
| TScheduler | **Когда** (расписание) | Cron 0 9 * * 1 |
| TBehavior | **Что делать** (логика) | SendEmailJob |
| TTune | **Нюансы** (пороги) | Timeout=5000 |
| Left | По левому краю | Текст |
| Center | По центру | Текст |
| Right | По правому краю | 123.45 |

- [Start](#start)
  - [1. Правильная топология в Trading.AppHost/Program.cs](#1-правильная-топология-в-tradingapphostprogramcs)
  - [2. Trading.Gateway уже использует YARP](#2-tradinggateway-уже-использует-yarp)

```text
Trading.Web -> Trading.Gateway -> Trading.Api.Orders
Trading.Gateway -> Trading.Api.MarketData
```
