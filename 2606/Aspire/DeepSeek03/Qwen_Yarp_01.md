# 2 YARP

## YARP (Yet Another Reverse Proxy)

**YARP (Yet Another Reverse Proxy)** — это высокопроизводительная библиотека от Microsoft для создания гибких обратных прокси-серверов на платформе .NET. 

В нашей архитектуре YARP развернут внутри проекта **Trading.Gateway** и выполняет стратегическую роль центрального L7-маршрутизатора для всех входящих HTTP-запросов.

### Что делает YARP в нашем приложении

Если коротко, он полностью изолирует внутренний контур от внешнего мира и распределяет трафик на основе путей:

Используйте код с осторожностью.

Trading.Web
|
| HTTP-запрос (например, к /api/orders)
v
Trading.Gateway + YARP
|
| Анализирует URI запроса
|
+--> /api/orders/*-> orders-api (Внутренний кластер)
+--> /api/marketdata/* -> market-data-api (Внутренний кластер)

Благодаря этому фронтенд (`Trading.Web`) физически лишен прямой связи с `Orders API` или `MarketData API`. Он взаимодействует исключительно с `Gateway`, делегируя YARP право принимать решения о конечной пересылке пакетов.

### Где именно это подключено

Вся инициализация прокси-движка сосредоточена в файле `Trading.Gateway/Program.cs`:

```csharp
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();
```
---
Разберем эту конфигурационную цепочку по частям.
Вводный блок по YARP успешно размечен в нашем минималистичном стиле.
Присылайте следующий фрагмент, где идет подробный разбор конфигурационной цепочки по частям (разбор LoadFromConfig, AddServiceDiscoveryDestinationResolver и секций appsettings.json)!

#### 1. AddReverseProxy()

Регистрирует базовые компоненты и сервисы YARP в DI-контейнере приложения:

```csharp
builder.Services.AddReverseProxy()
```

С этого момента наше приложение получает возможности полноценного прокси-сервера: оно может принимать входящие HTTP-запросы из внешней сети, анализировать их и транслировать во внутренний контур распределенной системы.

---

#### 2. LoadFromConfig(...)

Загружает декларативные правила маршрутизации трафика и конфигурацию кластеров из конфигурационного файла приложения:

```csharp
.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
```

Все правила считываются из секции `ReverseProxy` в файле `Trading.Gateway/appsettings.json`. В нем детально описана карта маршрутов (Routes):

```json
"ReverseProxy": {
  "Routes": {
    "orders-route-base": {
      "ClusterId": "orders-cluster",
      "Match": {
        "Path": "/api/orders"
      }
    },
    "orders-route": {
      "ClusterId": "orders-cluster",
      "Match": {
        "Path": "/api/orders/{**catch-all}"
      }
    },
    "marketdata-route-base": {
      "ClusterId": "marketdata-cluster",
      "Match": {
        "Path": "/api/marketdata"
      }
    },
    "marketdata-route": {
      "ClusterId": "marketdata-cluster",
      "Match": {
        "Path": "/api/marketdata/{**catch-all}"
      }
    }
  }
}
```

На основе этой конфигурации движок YARP принимает однозначные L7-решения:

* Если URI запроса строго соответствует `/api/orders` или подпадает под маску `/api/orders/{**catch-all}` $\rightarrow$ перенаправить трафик в **orders-cluster**.
* Если URI запроса строго соответствует `/api/marketdata` или подпадает под маску `/api/marketdata/{**catch-all}` $\rightarrow$ перенаправить трафик в **marketdata-cluster**.

#### 3. AddServiceDiscoveryDestinationResolver()

Критически важный интеграционный шаг для экосистемы .NET Aspire:

```csharp
.AddServiceDiscoveryDestinationResolver();
```

Этот метод подключает механизм динамического обнаружения сервисов (Service Discovery). Благодаря ему YARP сопоставляет абстрактные доменные имена целевых систем с их реальными физическими адресами и портами в рантайме.

В секции кластеров (Clusters) файла `appsettings.json` адреса назначения указаны через логические идентификаторы:

```json
"Clusters": {
  "orders-cluster": {
    "Destinations": {
      "orders-destination": {
        "Address": "http://orders-api/"
      }
    }
  },
  "marketdata-cluster": {
    "Destinations": {
      "marketdata-destination": {
        "Address": "http://market-data-api/"
      }
    }
  }
}
```

Адреса `http://orders-api/` и `http://market-data-api/` не привязаны к конкретному сетевому интерфейсу (вроде `http://localhost:5260`). Это строковые псевдонимы ресурсов, которые вы задекларировали в оркестраторе `Trading.AppHost/Program.cs`:

```csharp
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api");
var marketDataApi = builder.AddProject<Projects.Trading_Api_MarketData>("market-data-api");
```

И YARP через Service Discovery может найти реальный адрес этих сервисов во время выполнения.

В момент выполнения запроса плагин `DestinationResolver` перехватывает имя `"orders-api"`, запрашивает у инфраструктуры Aspire его текущие сетевые координаты и подменяет адрес на актуальный "на лету".

### Что конкретно делает YARP при обработке запроса

Давайте пошагово проследим сквозной путь выполнения запроса на конкретном примере. Пользователь находится на UI-странице `Trading.razor` и нажимает кнопку **«Загрузить AAPL»**:

1. **Инициация вызова на фронтенде**  

   Компонент UI вызывает метод API-клиента:

   ```csharp
   await Api.GetMarketDataAsync("AAPL");
   ```

2. **Формирование HTTP-пакета**  
   Внутри типизированного клиента `TradingApiClient.cs` выполняется стандартный асинхронный GET-запрос:

   ```csharp
   public async Task<MarketData?> GetMarketDataAsync(string symbol)
   {
       return await _http.GetFromJsonAsync<MarketData>(\$"/api/marketdata/{symbol}");
   }
   ```

   Поскольку базовый адрес (`BaseAddress`) для `HttpClient` настроен на шлюз, исходящий запрос физически отправляется по адресу:

   ```http
   http://gateway/api/marketdata/AAPL
   ```

3. **Прием и L7-анализ на стороне Gateway**  
   Проект `Trading.Gateway` принимает входящий поток. Движок YARP перехватывает URI и анализирует его текстовую структуру. Он видит, что путь начинается с `/api/marketdata`.

4. **Матчинг маршрута (Route Matching)**  
   YARP сканирует карту маршрутов в конфигурации и находит совпадение по маске `{**catch-all}`:

   ```json
   "marketdata-route": {
     "ClusterId": "marketdata-cluster",
     "Match": {
       "Path": "/api/marketdata/{**catch-all}"
     }
   }
   ```

   Маршрут указывает, что данный запрос закреплен за целевой группой серверов `marketdata-cluster`.

5. **Резолв адреса через Service Discovery**  
   YARP переходит к описанию указанного кластера:

   ```json
   "marketdata-cluster": {
     "Destinations": {
       "marketdata-destination": {
         "Address": "http://market-data-api/"
       }
     }
   }
   ```

   В этот момент активируется `ServiceDiscoveryDestinationResolver`. Он перехватывает логическое имя `http://market-data-api` и запрашивает у оркестратора .NET Aspire актуальные сетевые координаты (например, локальный IP и динамический порт Kestrel `http://127.0.0.1:54321`), на которых сейчас запущен инстанс `Trading.Api.MarketData`.

6. **Проксирование и обратный транзит**  
   YARP пересылает оригинальный HTTP-запрос на вычисленный физический адрес. Сервис рыночных данных обрабатывает его и возвращает JSON-ответ:

   ```json
   {
     "symbol": "AAPL",
     "price": 175.50,
     "timestamp": "2026-10-28T15:48:00Z"
   }
   ```

   YARP принимает этот payload от бэкенда и прозрачно транслирует его обратно по открытому сетевому соединению во фронтенд-приложение `Trading.Web`.

---

### Какие маршруты у нас настроены

На текущем этапе в системе жестко сконфигурированы два основных независимых направления (кластера) для обработки доменного трафика.

#### 1. Направление: Запросы к ордерам

Все запросы по путям `/api/orders` и `/api/orders/{**catch-all}` YARP перенаправляет на кластер **orders-api**.

Это трафик к проекту **Trading.Api.Orders**. На стороне бэкенда развернуты следующие эндпоинты Minimal APIs:

```csharp
app.MapGet("/api/orders", () => Results.Ok(orders));

app.MapPost("/api/orders", (Order order) =>
{
    order.Id = Guid.NewGuid();
    order.CreatedAt = DateTime.UtcNow;
    orders.Add(order);
    return Results.Created(\$"/api/orders/{order.Id}", order);
});
```

---

#### 2. Направление: Запросы к рыночным данным

Все запросы по путям `/api/marketdata` и `/api/marketdata/{**catch-all}` YARP перенаправляет на кластер **market-data-api**.

Этот поток закреплен за проектом **Trading.Api.MarketData**. Внутри него объявлены следующие эндпоинты:

```csharp
app.MapGet("/api/marketdata/{symbol}", (string symbol) =>
{
    if (marketData.TryGetValue(symbol.ToUpper(), out var price))
        return Results.Ok(new
        {
            Symbol = symbol.ToUpper(),
            Price = price,
            Timestamp = DateTime.UtcNow
        });

    return Results.NotFound(\$"Symbol {symbol} not found");
});

app.MapGet("/api/marketdata", () => Results.Ok(marketData));
```

---

### Зачем это нужно архитектурно

В нашей экосистеме YARP выступает главным инструментом материализации ключевой идеи: **сервисы не должны общаться хаотично по принципу «каждый с каждым».**

Вместо запутанной паутины связей внедряется строгий центральный узел:

$$\text{ServiceA} \longleftrightarrow \text{Router} \longleftrightarrow \text{ServiceB}$$

В текущей кодовой базе роль такого «контроллера гравитации» берет на себя **Trading.Gateway**. YARP превращает стандартное веб-приложение ASP.NET Core в интеллектуальный обратный прокси-сервер (шлюз).

В результате мы получаем прозрачное и контролируемое дерево распределения трафика:

Web
|
v
Gateway
|
+--> Orders
|
+--> MarketData

Это базовый элемент устойчивой топологии **Hub-and-Spoke** («Звезда»).
Блок с направлениями маршрутизации и архитектурным обоснованием YARP полностью готов.

### Важный нюанс: пока YARP у нас «тупой»

Сейчас YARP в основном выполняет роль прозрачного проксировщика. Он работает по линейной схеме:

* Пришел запрос `/api/orders`? $\rightarrow$ Перешли в **Orders API**.
* Пришел запрос `/api/marketdata`? $\rightarrow$ Перешли в **MarketData API**.

Шлюз пока не берет на себя сложную сквозную бизнес-логику:

* Не агрегирует данные из разных источников;
* Не проверяет права пользователя (авторизация на периметре);
* Не принимает команды через паттерн Transactional Outbox;
* Не возвращает мгновенный статус `202 Accepted` для асинхронных мутаций;
* Не склеивает ответы нескольких бэкенд-сервисов в один DTO;
* Не управляет идемпотентностью входящих транзакций.

То есть сейчас этот проект — прочный инфраструктурный фундамент для будущего `CentralRouter`, но еще не полностью «умный» распределенный координатор.

### Почему это хорошо

Такой подход позволяет сделать первый уверенный шаг в сторону распределенной архитектуры без преждевременного усложнения:

$$\text{HTTP} \rightarrow \text{Gateway} \rightarrow \text{Внутренние сервисы}$$

Если бы мы не внедрили YARP на старте, нам пришлось бы выбирать между тремя плохими альтернативами:
* Заставить фронтенд напрямую знать физические адреса и порты всех микросервисов, ломая инкапсуляцию;
* Писать прокси-эндпоинты и контроллеры вручную через `IHttpClientFactory`, плодя шаблонный код (boilerplate);
* Сразу тащить в проект тяжелые асинхронные брокеры сообщений там, где в них еще нет жесткой бизнес-необходимости.

Используя YARP, мы изящно закрываем базовые потребности и резервируем плацдарм для эволюции системы:

1. **Единая точка входа** — у клиента есть только один сетевой адрес для работы со всем бэкендом.
2. **Изоляция фронтенда** — внутренняя структура сети и доменные сервисы скрыты за периметром шлюза.
3. **Централизованная маршрутизация** — управление потоками трафика декларативно описывается в одном месте.
4. **Готовность к агрегации** — база для быстрого превращения прокси в «умный» BFF-композитор.
5. **Готовность к «Броне»** — архитектурный задел для точечного внедрения Outbox или WolverineFx на шлюзе.
6. **Нативная наблюдаемость** — сквозные трейсы вызовов доступны в Aspire Dashboard из коробки.

