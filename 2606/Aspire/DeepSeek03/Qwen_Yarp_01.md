# 2 YARP

---

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

### Дополнительный диагностический эндпоинт

В файле `Trading.Gateway/Program.cs` развернут полезный инструмент для инспекции рантайм-конфигурации шлюза:

```csharp
app.MapGet("/debug/routes", (IProxyConfigProvider configProvider) =>
{
    var config = configProvider.GetConfig();

    return Results.Ok(new
    {
        Routes = config.Routes.Select(r => new
        {
            r.RouteId,
            Path = r.Match.Path,
            r.ClusterId
        }),
        Clusters = config.Clusters.Select(c => new
        {
            c.ClusterId,
            Destinations = c.Destinations?.Select(d => new
            {
                d.Key,
                d.Value.Address
            })
        })
    });
});
```

Этот эндпоинт позволяет в реальном времени проверить, какие именно правила маршрутизации и целевые кластеры YARP загрузил из конфигурационных файлов или динамических провайдеров.

Если отправить GET-запрос на адрес:
`/debug/routes`

Шлюз вернет структурированный JSON со слепком текущего состояния топологии сети:

```json
{
  "routes": [
    {
      "routeId": "orders-route-base",
      "path": "/api/orders",
      "clusterId": "orders-cluster"
    },
    {
      "routeId": "orders-route",
      "path": "/api/orders/{**catch-all}",
      "clusterId": "orders-cluster"
    },
    {
      "routeId": "marketdata-route-base",
      "path": "/api/marketdata",
      "clusterId": "marketdata-cluster"
    },
    {
      "routeId": "marketdata-route",
      "path": "/api/marketdata/{**catch-all}",
      "clusterId": "marketdata-cluster"
    }
  ],
  "clusters": [
    {
      "clusterId": "orders-cluster",
      "destinations": [
        {
          "key": "orders-destination",
          "address": "http://orders-api/"
        }
      ]
    },
    {
      "clusterId": "marketdata-cluster",
      "destinations": [
        {
          "key": "marketdata-destination",
          "address": "http://market-data-api/"
        }
      ]
    }
  ]
}
```

Это крайне эффективный инструмент для быстрой диагностики конфигурации на ранних этапах разработки, помогающий мгновенно проверить корректность работы сопоставления путей и адресов.

### Итог по работе YARP

В текущей кодовой базе прокси-движок YARP выполняет строго определенный цикл операций:

Принимает HTTP-запросы от фронтенда (Trading.Web)
|
v
Смотрит на относительный путь (URI)
|
v
Решает, какой внутренний сервис должен обработать запрос
|
v
Находит адрес и пересылает запрос в Orders API или MarketData API
|
v
Возвращает полученный ответ обратно клиенту

То есть в текущей архитектуре:
**YARP — это надежный механизм обратной проксировки внутри проекта Trading.Gateway.** 

Он успешно реализует первую, базовую ступень вашей идеи центрального роутера:

$$\text{Web} \rightarrow \text{Gateway} \rightarrow \text{Внутренние сервисы}$$

Однако в целевом состоянии `Trading.Gateway` должен перерасти роль «тупого» прокси-сервера и превратиться в полноценный интеллектуальный узел системы, который возьмет на себя функции:

* **Агрегатора данных (BFF)** для уменьшения количества сетевых запросов от фронтенда;
* **Единой точки приема команд** с первичной бизнес-валидацией;
* **Места подключения Transactional Outbox** («Брони») для гарантированной асинхронной доставки;
* **Периметра управления политикой доступов** и авторизации;
* **Главного центра наблюдаемости** сквозных бизнес-процессов.

Использование YARP на старте — это отличная, проверенная инженерная ступень для безболезненного построения такой архитектуры.

Раздел, посвященный YARP, полностью сформирован и структурирован.

### Вопрос

Можно ли в контексте одного Aspire создавать несколько Gateways для того чтобы сделать несколько «Облаков сервисов»? Чтобы у каждого Облака был свой Gateway?

### Ответ

**Да. В рамках одного Aspire-приложения можно создать несколько Gateway’ов и сгруппировать вокруг них изолированные логические «облака сервисов».**

Более того, это является классическим и очень правильным архитектурным развитием вашей модели при масштабировании системы:

Aspire AppHost
|
+-- Cloud A (Домен Торговли)
|       |
|       +-- Gateway A
|       +-- Service A1
|       +-- Service A2
|
+-- Cloud B (Домен Рыночных Данных)
|       |
|       +-- Gateway B
|       +-- Service B1
|       +-- Service B2
|
+-- Cloud C (Домен Аналитики)
|
+-- Gateway C
+-- Service C1
+-- Service C2

При этом важно сразу разделять два аспекта:

1. **Технически:** платформа .NET Aspire никак не ограничивает количество регистрируемых инстансов Gateway/YARP.
2. **Архитектурно:** к такому разделению стоит переходить только тогда, когда у вас действительно появляются независимые доменные области, автономные команды разработки, разные уровни критичности доступа (Security Tiers) или уникальные сценарии независимого масштабирования.

---

### Как это выглядит в коде и топологии Aspire

Давайте сравним текущую структуру с возможными вариантами усложнения. Сейчас ваша архитектура выглядит так:

Trading.Web
|
v
Trading.Gateway
|
+--> Trading.Api.Orders
|
+--> Trading.Api.MarketData

#### Вариант 1. Раздельные доменные шлюзы (Параллельные облака)

Если вы хотите разделить ответственность бэкендов, можно ввести несколько равноправных шлюзов:

Trading.Web
|
+--> Trading Gateway
|        |
|        +--> Orders API
|
+--> Market Data Gateway
|
+--> MarketData API

#### Вариант 2. Двухуровневый шлюз (Hierarchical Gateways)

Для крупных систем чаще всего применяется иерархический подход, где один внешний шлюз (Edge) управляет потоками, а внутренние шлюзы поглощают доменную сложность:

Trading.Web
|
v
Edge Gateway (Внешний контур)
|
+--> Trading Gateway (Облако А)
|        |
|        +--> Orders API
|
+--> Market Data Gateway (Облако Б)
|
+--> MarketData API

#### 2. Пример реализации в AppHost

Допустим, вы приняли решение изолировать систему и разбить её на два автономных контура: **Trading Cloud** и **MarketData Cloud**. 

В оркестраторе `Trading.AppHost/Program.cs` эта декларативная топология будет выглядеть следующим образом:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// --- Сервисы облака Трейдинга (Trading Cloud) ---
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api");

// --- Сервисы облака Рыночных Данных (MarketData Cloud) ---
var marketDataApi = builder.AddProject<Projects.Trading_Api_MarketData>("market-data-api");

// --- Шлюз облака Трейдинга ---
var tradingGateway = builder.AddProject<Projects.Trading_Gateway_Trading>("trading-gateway")
    .WithReference(ordersApi);

// --- Шлюз облака Рыночных Данных ---
var marketDataGateway = builder.AddProject<Projects.Trading_Gateway_MarketData>("marketdata-gateway")
    .WithReference(marketDataApi);

// --- Фронтенд (Web) — авторизован коммуницировать с обоими шлюзами ---
var web = builder.AddProject<Projects.Trading_Web>("web")
    .WithReference(tradingGateway)
    .WithReference(marketDataGateway);

builder.Build().Run();
```

При такой конфигурации мы получаем строго детерминированные пути прохождения трафика:

* $$\text{Web} \rightarrow \text{trading-gateway} \rightarrow \text{orders-api}$$
* $$\text{Web} \rightarrow \text{marketdata-gateway} \rightarrow \text{market-data-api}$$

##### Архитектурные гарантии изоляции:

* `web` физически **не видит** бэкенды `orders-api` и `market-data-api` напрямую.
* `trading-gateway` полностью изолирован и **не имеет путей** к `market-data-api`.
* `marketdata-gateway` изолирован и **не может обратиться** к `orders-api`.

Жесткая физическая граница между вашими независимыми «облаками сервисов» полностью задается на уровне компиляции кода через декларативные методы `.WithReference()`.

---

#### 3. Можно ли использовать один и тот же проект Gateway несколько раз?

Технически можно пойти по пути повторного использования одной и той же кодовой базы:

```csharp
var tradingGateway = builder.AddProject<Projects.Trading_Gateway>("trading-gateway");
var marketGateway = builder.AddProject<Projects.Trading_Gateway>("market-gateway");
```

То есть бинарный код `Trading.Gateway` один и тот же, но в топологии .NET Aspire регистрируются два независимых изолированных runtime-ресурса.

Этот сценарий абсолютно валиден, если вы будете разделять их поведение динамически через переменные окружения:

```csharp
var tradingGateway = builder.AddProject<Projects.Trading_Gateway>("trading-gateway")
    .WithEnvironment("GATEWAY_PROFILE", "trading")
    .WithReference(ordersApi);

var marketGateway = builder.AddProject<Projects.Trading_Gateway>("market-gateway")
    .WithEnvironment("GATEWAY_PROFILE", "marketdata")
    .WithReference(marketDataApi);
```

А на стороне самого приложения `Trading.Gateway` в файле `Program.cs` перехватывать этот профиль и подтягивать соответствующую карту маршрутизации:

```csharp
var profile = builder.Configuration["GATEWAY_PROFILE"];
builder.Configuration.AddJsonFile(\$"appsettings.{profile}.json", optional: false);
```

##### Рекомендация по декомпозиции

Несмотря на изящность динамической конфигурации, на этапе становления системы гораздо надежнее использовать явный, физически разделенный путь:

* `Trading.Gateway.Trading`
* `Trading.Gateway.MarketData`
* `Trading.Gateway.Admin`

Создание отдельных физических проектов под каждый доменный шлюз дает весомые преимущества:

* **Наглядность в решении (Solution):** структура папок сразу отражает архитектурную мысль.
* **Изоляция в Aspire Dashboard:** каждый шлюз отображается как уникальный тип компонента.
* **Чистота конфигурации:** у каждого шлюза свой собственный, изолированный `appsettings.json`, исключающий риск перекрестного загрязнения маршрутов.
* **Простота рассуждения:** разработчикам гораздо легче контролировать и развивать жестко очерченные доменные границы ответственности.

---

#### 4. Что получится в Aspire Dashboard

Каждый шлюз будет зарегистрирован как самостоятельный первоклассный runtime-ресурс:

* `trading-gateway`
* `marketdata-gateway`
* `orders-api`
* `market-data-api`
* `web`

И это огромное преимущество для эксплуатации. Вы получите изолированную диагностику для каждого доменного контура:

* **Раздельные логи:** потоки вывода каждого Gateway не перемешиваются в одну кучу.
* **Изолированные трейсы (Traces):** сквозная трассировка четко разделяется на независимые ветви.
* **Индивидуальные Health Checks:** падение одного шлюза не переводит всю панель в критическое состояние.
* **Локализация ошибок:** метрики отказов (`5xx` коды) собираются точечно по каждому направлению.
* **Явный граф зависимостей:** интерактивная карта Aspire Dashboard наглядно отрисует два чистых изолированных маршрута:

$$\text{Web} \rightarrow \text{trading-gateway} \rightarrow \text{orders-api}$$

$$\text{Web} \rightarrow \text{marketdata-gateway} \rightarrow \text{market-data-api}$$

Это полностью соответствует вашей фундаментальной идее контролируемой топологии.

---

#### 5. Как фронтенд должен ходить в несколько Gateway’ов

Если в системе сосуществуют несколько доменных шлюзов, для организации клиентского взаимодействия есть два основных пути.

##### Вариант 1. Фронтенд знает несколько Gateway’ов напрямую

В этом сценарии в проекте `Trading.Web` регистрируются два независимых типизированных клиента, каждый из которых нацелен на свой целевой шлюз:

```csharp
builder.Services.AddHttpClient<TradingApiClient>(client =>
{
    client.BaseAddress = new Uri("http://trading-gateway");
});

builder.Services.AddHttpClient<MarketDataApiClient>(client =>
{
    client.BaseAddress = new Uri("http://marketdata-gateway");
});
```

Таким образом:

* `TradingApiClient` транслирует команды в **trading-gateway**.
* `MarketDataApiClient` запрашивает котировки из **marketdata-gateway**.

* **Преимущества:** Максимальная простота реализации и линейная прозрачность.
* **Недостатки:** Фронтенд вынужден управлять жизненным циклом и адресами нескольких точек входа. Нарушается принцип единого окна, что при росте количества облаков усложняет клиентский код.

---

##### Вариант 2. Единый Edge Gateway (Иерархическая маршрутизация)

Более зрелый и промышленный архитектурный вариант заключается в развертывании зонтичного шлюза на самом внешнем периметре системы (Edge):

Trading.Web
|
v
Edge Gateway (Внешний контур)
|
+--> Trading Gateway (Внутреннее облако А)
|
+--> MarketData Gateway (Внутреннее облако Б)

В этой конфигурации фронтенд по-прежнему оперирует концепцией единственного логического адреса:

```csharp
builder.Services.AddHttpClient<TradingApiClient>(client =>
{
    client.BaseAddress = new Uri("http://edge-gateway");
});
```

Вся интеллектуальная маршрутизация делегируется `Edge Gateway`:

* `/api/trading/*` $\rightarrow$ пересылается во внутренний **trading-gateway**.
* `/api/marketdata/*` $\rightarrow$ пересылается во внутренний **marketdata-gateway**.

##### Почему этот вариант предпочтительнее для крупных систем

* **Абсолютная инкапсуляция:** Фронтенд знает строго одну точку входа, внутренние облака сервисов полностью скрыты за периметром.
* **Эволюционная гибкость:** Можно дробить, объединять или переносить внутренние доменные облака и шлюзы, не меняя и не перекомпилируя код клиента.
* **Один сквозной рубеж:** На уровне `Edge Gateway` максимально удобно внедрять сквозные политики безопасности (OAuth/OIDC), глобальный Rate Limiting, единое логирование и защиту от DDoS-атак.

Варианты интеграции фронтенда с мульти-шлюзовой архитектурой успешно переведены в Markdown.

---

#### 6. Пример иерархии с Edge Gateway

Полная схема прохождения трафика при двухуровневом шлюзовании выглядит следующим образом:

Browser
|
v
Trading.Web
|
v
Edge Gateway (Единая точка входа)
|
+---------------------------+
|                           |
v                           v
Trading Gateway          MarketData Gateway
|                           |
v                           v
Orders API             MarketData API

В оркестраторе `Trading.AppHost/Program.cs` эта многодоменная иерархическая структура описывается лаконичным декларативным кодом:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// --- Внутренние доменные сервисы ---
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api");
var marketDataApi = builder.AddProject<Projects.Trading_Api_MarketData>("market-data-api");

// --- Внутренние шлюзы облаков (Изолированные доменные шлюзы) ---
var tradingGateway = builder.AddProject<Projects.Trading_Gateway_Trading>("trading-gateway")
    .WithReference(ordersApi);

var marketDataGateway = builder.AddProject<Projects.Trading_Gateway_MarketData>("marketdata-gateway")
    .WithReference(marketDataApi);

// --- Внешний Edge Gateway (Зонтичный шлюз системы) ---
var edgeGateway = builder.AddProject<Projects.Trading_Gateway_Edge>("edge-gateway")
    .WithReference(tradingGateway)
    .WithReference(marketDataGateway);

// --- Фронтенд (Web) — имеет доступ только к внешнему периметру ---
var web = builder.AddProject<Projects.Trading_Web>("web")
    .WithReference(edgeGateway);

builder.Build().Run();
```

При такой конфигурации рантайм-среда гарантирует строгое соблюдение цепочек вызовов:

* $$\text{Web} \rightarrow \text{edge-gateway} \rightarrow \text{trading-gateway} \rightarrow \text{orders-api}$$
* $$\text{Web} \rightarrow \text{edge-gateway} \rightarrow \text{marketdata-gateway} \rightarrow \text{market-data-api}$$

Это готовый шаблон для построения **серьезной многодоменной enterprise-архитектуры**, обеспечивающий абсолютную изоляцию внутренних компонентов при сохранении сквозной прозрачности и наблюдаемости в экосистеме .NET Aspire.

Иерархическая схема шлюзования успешно зафиксирована.

---

#### 7. Что нужно настроить в YARP для каждого Gateway

У каждого Gateway должна быть своя изолированная конфигурация маршрутов.

Пример для **Trading.Gateway.Trading/appsettings.json**:

```json
{
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
      }
    },
    "Clusters": {
      "orders-cluster": {
        "Destinations": {
          "orders-destination": {
            "Address": "http://orders-api"
          }
        }
      }
    }
  }
}
```

Пример для **Trading.Gateway.MarketData/appsettings.json**:

```json
{
  "ReverseProxy": {
    "Routes": {
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
    },
    "Clusters": {
      "marketdata-cluster": {
        "Destinations": {
          "marketdata-destination": {
            "Address": "http://market-data-api"
          }
        }
      }
    }
  }
}
```

Если на внешнем периметре появляется зонтичный **Edge Gateway**, его конфигурация в **Trading.Gateway.Edge/appsettings.json** будет связывать внешние пути с внутренними шлюзами облаков:

```json
{
  "ReverseProxy": {
    "Routes": {
      "trading-route": {
        "ClusterId": "trading-gateway-cluster",
        "Match": {
          "Path": "/api/trading/{**catch-all}"
        }
      },
      "marketdata-route": {
        "ClusterId": "marketdata-gateway-cluster",
        "Match": {
          "Path": "/api/marketdata/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "trading-gateway-cluster": {
        "Destinations": {
          "trading-gateway-destination": {
            "Address": "http://trading-gateway"
          }
        }
      },
      "marketdata-gateway-cluster": {
        "Destinations": {
          "marketdata-gateway-destination": {
            "Address": "http://marketdata-gateway"
          }
        }
      }
    }
  }
}
```

##### Анализ сквозного прохождения трафика:

В такой конфигурации внешние пути для клиентского приложения выглядят единообразно:

* `/api/trading/orders`
* `/api/marketdata/AAPL`

Маршрутизация внутри защищенного контура распределяется следующим образом:

* `/api/trading/orders` $\rightarrow$ пересылается на **trading-gateway**
* `/api/marketdata/AAPL` $\rightarrow$ пересылается на **marketdata-gateway**

#### 8. Важный момент: префиксы маршрутов

При проектировании архитектуры с несколькими доменными облаками критически важно полностью исключить пересечение (коллизии) путей маршрутизации.

❌ **Плохой пример (пересечение контекстов):**

* **Cloud A:** `/api/orders`
* **Cloud B:** `/api/orders`

При такой конфигурации внешний `Edge Gateway` не сможет однозначно определить целевой кластер, что приведет к ошибкам маршрутизации или непредсказуемому перехвату трафика.

===

✔️ **Правильный подход (изоляция через префиксы):**

* **Cloud A (Трейдинг):** `/api/trading/orders`
* **Cloud B (Котировки):** `/api/marketdata/quotes`
* **Cloud C (Риски):** `/api/risk/limits`

Каждому автономному контуру бэкенда должен соответствовать свой уникальный, семантически понятный глобальный префикс:

* `/trading/*` — торговые операции и транзакции
* `/marketdata/*` — потоки рыночных котировок и тиков
* `/risk/*` — проверки лимитов и комплаенс
* `/portfolio/*` — состояние позиций и балансов пользователей
* `/admin/*` — административные интерфейсы и управление

Внедрение уникальных префиксов на уровне доменов делает всю распределенную систему прозрачной, гарантирует отсутствие коллизий в YARP и значительно упрощает чтение трейсов при диагностике.

#### 9. Когда несколько Gateway’ов действительно нужны

Переход от одного шлюза к мульти-шлюзовой архитектуре оправдан только при наличии четких физических или организационных границ. Внедрять несколько Gateway’ов имеет смысл в следующих сценариях:

1. **Разные бизнес-домены**
   Когда система четко бьется на крупные, независимые контексты:
   * `Trading Cloud` — транзакции и исполнение;
   * `MarketData Cloud` — котировки и стаканы;
   * `Risk Cloud` — комплаенс и валидация;
   * `Portfolio Cloud` — балансы и позиции;
   * `Billing Cloud` — взаиморасчеты.
   Каждое облако живет по своим правилам, имеет свои контракты и изолированный жизненный цикл.

2. **Разные команды разработки**
   Если за блок ордеров отвечает одна команда, за рыночные данные — вторая, а за риски — третья. Отдельные шлюзы позволяют командам полностью владеть своими маршрутами и деплоить изменения независимо, не рискуя сломать чужую конфигурацию.

3. **Разные профили и требования к нагрузке**
   * **MarketData Gateway** испытывает колоссальную high-load нагрузку из-за миллионов тиков, обновлений стаканов и графиков. Здесь важна пропускная способность.
   * **Orders Gateway** может иметь низкую частоту запросов, но критические требования к надежности, транзакционности, защите от дублирования (идемпотентности) и времени отклика.
   Разделение шлюзов защищает критически важные транзакции от влияния высоконагруженного трафика котировок.

4. **Разные требования к безопасности и изоляции**
   * **Admin Gateway** требует жесткой корпоративной авторизации, обязательного детального аудита каждого действия, подключения к системам предотвращения утечек и ограничения доступа по белым спискам IP-адресов.
   * **Public MarketData Gateway** открыт для всех клиентов и требует лишь базовой защиты от DDoS и адекватного Rate Limiting.

5. **Версионирование контрактов при плавной миграции**
   Когда необходимо запустить новую версию доменного API без остановки старой:
   * `Trading Gateway v1` (обслуживает `/api/v1/trading/*`);
   * `Trading Gateway v2` (обслуживает `/api/v2/trading/*`).
   Это позволяет полностью изолировать логику обработки устаревших и новых контрактов на сетевом периметре.

Блок с критериями разделения шлюзов успешно добавлен в манифест.

#### 10. Когда несколько Gateway’ов не нужны

Не стоит искусственно плодить шлюзы только ради модного слова «облако» или микросервисного тренда. 

Если на текущем этапе ваша система состоит из базового набора компонентов:

* `Orders API`
* `MarketData API`
* `Trading.Web`

И всё это разрабатывается одной небольшой командой как единый продукт, то **одного центрального Gateway более чем достаточно**.

Текущая минималистичная схема:

Trading.Web
|
v
Trading.Gateway (Единый роутер)
|
+--> Orders API
|
+--> MarketData API

Такая топология на старте гораздо эффективнее, понятнее и легче в отладке. Помните, что каждый лишний шлюз в цепочке неизбежно добавляет накладные расходы:

* Больше сетевых прыжков (Network Hops) и скрытых задержек (Latency);
* Больше конфигурационных файлов и дублирования секций YARP;
* Больше потенциальных точек отказа (Single Points of Failure);
* Более сложные и ветвистые сквозные трассировки в дашборде;
* Усложнение процессов развертывания (CI/CD) и оркестрации;
* Высокий риск запутаться в перенаправлениях и префиксах маршрутов.

**Правило простое:** Держите архитектуру плоской и монолитной по шлюзам до тех пор, пока боль от управления одним роутером не превысит сложность поддержки нескольких.

Мы успешно зафиксировали все за и против мульти-шлюзовой архитектуры.

#### 11. Опасность: «спаситель от спагетти» может сам стать спагетти

Один централизованный шлюз гарантированно спасает систему от классического хаоса межсервисных связей:

$$\text{ServiceA} \longleftrightarrow \text{ServiceB} \longleftrightarrow \text{ServiceC}$$

Однако если начать плодить множество доменных шлюзов без жесткой архитектурной дисциплины, система мгновенно породит новый, еще более опасный антипаттерн — **гейтвейное спагетти**:

$$\text{GatewayA} \longleftrightarrow \text{GatewayB} \longleftrightarrow \text{GatewayC} \longleftrightarrow \text{GatewayA}$$

Когда для выполнения одного действия трафик начинает циркулировать по цепочке вида $\text{Web} \rightarrow \text{GatewayA} \rightarrow \text{GatewayB} \rightarrow \text{GatewayC}$, отладка и контроль задержек становятся невозможными.

##### Жесткие правила межоблачного взаимодействия

Чтобы предотвратить сетевой хаос, при проектировании нескольких облаков необходимо на уровне манифеста `AppHost` закрепить следующие правила:

1. ❌ **Полный запрет прямых междоменных связей:**  
   Компоненты из разных контекстов никогда не должны вызывать друг друга напрямую в обход периметра.  
   $$\text{Cloud A Service} \,\,\cancel{\longleftrightarrow}\,\, \text{Cloud B Service}$$

2. ✔️ **Синхронный транзит через шлюзы (Request-Response):**  
   Если сервису из Облака А критически важно получить данные из Облака Б в реальном времени, запрос обязан пройти строгий маршрут через авторизованные точки стыка:  
   $$\text{Cloud A Service} \longrightarrow \text{Cloud A Gateway} \longrightarrow \text{Cloud B Gateway} \longrightarrow \text{Cloud B Service}$$

3. ✔️ **Асинхронная декларативная интеграция (Рекомендуется):**  
   Для мутирующих операций идеальным решением является полная изоляция через шину или гарантированную доставку сообщений, где бэкенды общаются только через события:  
   $$\text{Cloud A Service} \longrightarrow \text{Event / Outbox} \longrightarrow \text{Cloud B Service}$$

Соблюдение этих правил гарантирует, что ваши шлюзы останутся инструментами изоляции сложности, а не превратятся в запутанный транзитный лабиринт.

Раздел угроз и правил для мульти-шлюзовой архитектуры успешно дополнен.
Мы полностью зафиксировали концептуальный пласт по YARP и «облакам сервисов».

#### 12. Как связывать облака между собой

Если вы разделяете систему на несколько изолированных «облаков», неизбежно возникает ключевой вопрос: как сервисы из одного контура должны взаимодействовать с сервисами из другого? 

Для решения этой задачи существует три проверенных архитектурных варианта.

---

##### Вариант 1. Маршрутизация через единый Edge Gateway

Трафик проходит сквозной путь через самый внешний зонтичный шлюз системы:

$$\text{Web} \longrightarrow \text{Edge Gateway} \longrightarrow \text{Domain Gateway} \longrightarrow \text{Service}$$

* **Когда применять:** Идеально подходит для классических клиентских сценариев, когда инициатором запроса выступает фронтенд или внешнее приложение.
* **Вердикт:** Для внутреннего межсервисного взаимодействия (Server-to-Server) этот путь избыточен. Он заставляет пакеты делать лишний сетевой прыжок через внешний контур, создавая неоправданную нагрузку на Edge-слой.

---

##### Вариант 2. Прямой вызов через Gateway целевого облака

Шлюз одного домена авторизуется в качестве легитимного клиента для шлюза другого домена:

$$\text{Trading Gateway} \longrightarrow \text{MarketData Gateway} \longrightarrow \text{MarketData Service}$$

В оркестраторе `Trading.AppHost/Program.cs` это право на междоменную коммуникацию закрепляется явным декларативным кодом:

```csharp
var tradingGateway = builder.AddProject<Projects.Trading_Gateway_Trading>("trading-gateway")
    .WithReference(ordersApi)
    .WithReference(marketDataGateway); // Явное разрешение видеть шлюз котировок
```

* **Когда применять:** Отличный прагматичный вариант для синхронного чтения данных (`Request-Response`) на начальных этапах развития системы. Например, когда домену торговли легитимно требуется быстро запросить текущие котировки.
* **Вердикт:** Это допустимо и безопасно, так как привилегия вызова не захардкожена в коде бэкенда, а контролируется централизованно в `AppHost`. Прямой доступ к сервисам за шлюзом по-прежнему закрыт.

---

##### Вариант 3. Изоляция через асинхронные события (Event-Driven)

Полный отказ от синхронных вызовов в пользу обмена сообщениями. Облака публикуют факты о произошедших изменениях в общую шину данных:

* `Trading Cloud` $\rightarrow$ публикует событие `OrderPlaced`
* `MarketData Cloud` $\rightarrow$ публикует событие `TradeExecuted`
* `Risk Cloud` $\rightarrow$ публикует событие `LimitUpdated`

* **Когда применять:** Это целевой вариант для последующих этапов масштабирования системы, особенно для мутирующих бизнес-операций (запись данных).
* **Вердикт:** На этом уровне в систему точечно и обоснованно внедряются тяжелые инфраструктурные компоненты: **Transactional Outbox**, **WolverineFx**, **RabbitMQ** или **NATS**. Но помните: *на старте проекта разворачивать этот слой не обязательно*. Начинать всегда лучше с простого и контролируемого Варианта 2.

Блок с вариантами интеграции облаков успешно размечен. Манифест по многошлюзовой архитектуре и YARP выглядит монолитно и завершенно.

---

#### 13. Несколько «облаков» в одном Aspire — это не физические облака

Важно четко разграничивать терминологию и не путать логическую декомпозицию с физическим разделением инфраструктуры.

Если внутри одного оркестратора `Trading.AppHost` вы объявляете ресурсы для `Cloud A`, `Cloud B` и `Cloud C`, то **это будут исключительно логические облака (контуры) внутри единого монолитного приложения оркестрации**.

Все эти доменные контуры по-прежнему функционируют внутри одного общего пространства управления:

* **Единый Aspire Control Plane** — оркестратор управляет запуском и остановкой всех процессов одновременно.
* **Общий Dashboard** — все метрики, логи и трейсы со всех шлюзов стекаются в одну панель.
* **Общий Service Discovery** — единая внутренняя адресная книга ресурсов рантайма.
* **Единый манифест** — на выходе генерируется один общий файл манифеста для развертывания (например, в Azure Container Apps).

##### Когда требуется настоящая физическая изоляция:

Если перед вашей архитектурой встанут требования полной автономности доменов (например, независимый жизненный цикл, разные циклы релизов команд, строгий сетевой файрвол между сегментами), логического разделения в `AppHost` станет недостаточно. Тогда придется разделять систему физически:

* Разворачивать **отдельные проекты AppHost** для каждого домена;
* Использовать **изолированные Kubernetes-кластеры** или разные пространства имен (Namespaces);
* Конфигурировать **независимые среды окружения** (Environments) и пайплайны CI/CD;
* Настраивать **жесткие сетевые политики (Network Policies)** на уровне облачного провайдера для физического запрета маршрутизации пакетов.

*Вердикт:* Для этапа активной разработки, локального проектирования и тестирования гибридных сценариев создание нескольких контролируемых логических контуров внутри одного `AppHost` — это великолепная, чистая и экономичная практика.

Концептуальное разграничение логических и физических облаков успешно добавлено в наш манифест.
Этот большой архитектурный блок по шлюзованию и топологии полностью завершен.

---

#### 14. Как это соотносится с вашей философией «Код как Конституция»

Ваша главная архитектурная идея звучит бескомпромиссно: **«Если связи нет в AppHost — её нет в реальности»**. Развертывание нескольких логических облаков не просто соотносится с этим правилом, а доводит его до абсолюта.

Давайте посмотрим на этот «закон» на уровне кода конфигурации:

```csharp
// --- Контур Торговли (Trading Cloud) ---
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api");
var tradingGateway = builder.AddProject<Projects.Trading_Gateway_Trading>("trading-gateway")
    .WithReference(ordersApi);

// --- Контур Рыночных Данных (MarketData Cloud) ---
var marketDataApi = builder.AddProject<Projects.Trading_Api_MarketData>("market-data-api");
var marketDataGateway = builder.AddProject<Projects.Trading_Gateway_MarketData>("marketdata-gateway")
    .WithReference(marketDataApi);
```

##### Сила архитектурного запрета:

При такой декларации рантайм-среда гарантирует железное соблюдение границ:

* `trading-gateway` физически **не имеет путей** к `market-data-api`.
* `marketdata-gateway` физически **не знает** о существовании `orders-api`.

Если разработчик в процессе написания фичи случайно или намеренно попытается сделать внутри `Trading.Gateway.Trading` прямой HttpClient-вызов:

```csharp
// Попытка обойти архитектурный регламент
await httpClient.GetFromJsonAsync
("http://market-data-api/api/marketdata/AAPL");
```

Этот код **гарантированно упадет с ошибкой в рантайме**. Инфраструктура .NET Aspire просто не инжектирует нужные переменные окружения и адреса для Service Discovery, так как в `AppHost` отсутствует явная декларативная привилегия:

```csharp
.WithReference(marketDataApi) // Без этой строки вызов физически невозможен
```

Это и есть ваш фундаментальный принцип в действии: **безопасность, предсказуемость и архитектурная чистота распределенной системы обеспечиваются не устными договоренностями в команде, а физическим отсутствием сетевых путей на уровне исполняемого кодекса инфраструктуры.**

Идеальное концептуальное завершение главы о философии «Кода как Конституции» готово.

---

#### 15. Что я бы сделал в вашем текущем проекте

Для текущего состояния кодовой базы я бы настоятельно рекомендовал **пока не усложнять топологию и не плодить много Gateway’ов**.

Сейчас у вас более чем достаточно одного центрального шлюза: **Trading.Gateway**. Внутренних сервисов всего два (`Orders API` и `MarketData API`), и один роутер идеально справляется с их оркестрацией, не создавая лишних накладных расходов.

Однако, чтобы не загнать себя в тупик при будущем масштабировании, **стоит заложить возможность безболезненного доменного разделения уже на этом этапе**. Для этого достаточно внедрить жесткую политику уникальных префиксов в адресах:

* `/api/trading/*` — для торговых операций;
* `/api/marketdata/*` — для рыночных котировок.

То есть в текущих эндпоинтах бэкенда маршруты будут выглядеть так:

* `/api/trading/orders`
* `/api/marketdata/{symbol}`

Если в будущем ваша система вырастет и вы решите физически разрезать инфраструктуру на изолированные контуры («облака»), вам не придется переписывать код бэкендов или логику фронтенда. Вы сможете просто разнести маршруты по отдельным шлюзам в конфигурации YARP:

* **Trading Gateway** полностью заберет под свой контроль префикс `/api/trading/*`.
* **MarketData Gateway** заберет под свой контроль префикс `/api/marketdata/*`.

Весь блок аналитики и проектирования шлюзов успешно переведен в Markdown!

---

#### 16. Рекомендуемая эволюция архитектуры шлюзования

Для последовательного развития распределенной системы без преждевременного усложнения (overengineering) рекомендуется придерживаться эволюционного подхода, состоящего из пяти последовательных этапов.

##### Этап 1. Один шлюз (Текущее состояние)

Топология представляет собой плоскую схему с единственным центральным элементом:

Web
 |
 v
Gateway
 |
 +--> Orders
 |
 +--> MarketData

**Основные цели этапа:**

* **Доказать работоспособность** базовой инфраструктуры распределенного приложения.
* **Наладить сквозную трассировку** и сбор метрик в единой панели.
* **Полностью исключить прямые связи** по принципу «каждый с каждым» на уровне бэкендов.
* **Получить единую точку входа** для клиентского приложения.

##### Этап 2. Логическое разделение маршрутов внутри одного шлюза

Физически в системе продолжает функционировать один проект `Trading.Gateway`, но на уровне конфигурации YARP и эндпоинтов бэкенда вводится жесткая изоляция контекстов через глобальные префиксы:

```http
/api/trading/*      -> Направление торговых операций
/api/marketdata/*   -> Направление рыночных данных
```

**Основные цели этапа:**

* Подготовить кодовую базу к безболезненному масштабированию.
* Явно очертить доменные зоны ответственности внутри конфигурационных файлов.
* Сохранить простоту развертывания (один процесс) при наличии выраженных логических границ.

##### Этап 3. Выделение выделенных доменных шлюзов

При разрастании кодовой базы, увеличении нагрузки или разделении зон ответственности между командами единый шлюз физически разделяется на два независимых runtime-компонента в топологии Aspire:

Web
 |
 +--> Trading Gateway

 |        |
 |        +--> Orders API
 |
 +--> MarketData Gateway
          |
          +--> MarketData API

**Основные цели этапа:**

* **Изолировать профили нагрузки:** защитить критически важный транзакционный трафик ордеров от высокочастотного потока рыночных котировок.
* **Обеспечить независимый деплой:** дать возможность командам изменять правила маршрутизации своих доменов без риска затронуть чужие сервисы.

##### Этап 4. Появление внешнего зонтичного шлюза (Edge Gateway)

Когда количество доменных шлюзов начинает расти, фронтенд-приложение освобождается от необходимости знать адреса каждого из них. На внешнем периметре разворачивается зонтичный `Edge Gateway`:

Web
 |
 v
Edge Gateway
 |
 +--> Trading Gateway
 |
 +--> MarketData Gateway

**Основные цели этапа:**

* **Вернуть инкапсуляцию периметра:** свести все клиентские вызовы к одной логической точке входа.
* **Централизовать сквозные политики:** вынести авторизацию (OAuth/OIDC), глобальный Rate Limiting и защиту от DDoS на единый внешний рубеж.

##### Этап 5. Переход к асинхронной федерации облаков

По мере усложнения бизнес-логики, появления тяжелых мутаций и повышения требований к отказоустойчивости, синхронное межсервисное взаимодействие минимизируется. Шлюзы превращаются в умные приемники команд, интегрированные с шаблонами гарантированной доставки:

Edge Gateway
    |
    +--> Trading Gateway
              |
              +--> Outbox (Гарантированная шина)
              |
              +--> Orders
              +--> Matching Engine
              +--> Risk Service

**Основные цели этапа:**

* **Обеспечить максимальную отказоустойчивость:** внедрить паттерн *Transactional Outbox* («Броня») непосредственно на уровне шлюза для мгновенного ответа клиенту (`202 Accepted`).
* **Разгрузить внутренний контур:** перевести тяжелые транзакции на асинхронные рельсы через WolverineFx, RabbitMQ или NATS.

#### 17. Практический шаблон реализации

Если сформулировать краткий и финальный ответ на вопрос о масштабировании:

**Да, в рамках одного .NET Aspire приложения можно создать несколько независимых Gateway’ов и сформировать вокруг них изолированные логические контуры («облака») сервисов.**

Для сохранения чистоты и понятности архитектуры рекомендуется строго придерживаться следующего правила декомпозиции:

$$\text{Одно логическое облако} \Longrightarrow \text{Один Gateway} \Longrightarrow \text{Один изолированный набор сервисов} \Longrightarrow \text{Один уникальный префикс маршрутов}$$

##### Фиксация архитектурных границ в AppHost

Связи и права доступа жестко закрепляются в коде оркестратора `Trading.AppHost/Program.cs`.

Пример декларации двух изолированных доменных облаков:

```csharp
// --- Доменное облако А (Cloud A) ---
var cloudAGateway = builder.AddProject<Projects.GatewayA>("cloud-a-gateway")
    .WithReference(serviceA1)
    .WithReference(serviceA2);

// --- Доменное облако Б (Cloud B) ---
var cloudBGateway = builder.AddProject<Projects.GatewayB>("cloud-b-gateway")
    .WithReference(serviceB1)
    .WithReference(serviceB2);
```

##### Конфигурация единой точки входа (Edge Gateway)

Если клиентскому приложению (фронтенду) требуется предоставить единый сетевой адрес для работы со всей системой, поверх доменных шлюзов разворачивается зонтичный шлюз:

```csharp
// --- Внешний Edge Gateway (Зонтичный шлюз системы) ---
var edgeGateway = builder.AddProject<Projects.EdgeGateway>("edge-gateway")
    .WithReference(cloudAGateway)
    .WithReference(cloudBGateway);

// --- Клиентское приложение (Web) — видит только внешний периметр ---
var web = builder.AddProject<Projects.Web>("web")
    .WithReference(edgeGateway);
```

Такой подход гарантирует стопроцентное соблюдение вашей философии: **если связь между компонентами не задекларирована в коде `AppHost`, она физически не сможет возникнуть в рантайме.**

#### 18. Главный архитектурный регламент

Выделение нескольких шлюзов — это мощный инструмент управления сложностью распределенной системы, но он требует жесткой дисциплины. Он ни в коем случае не должен превращаться в техническую самоцель или карго-культ микросервисов.

##### ❌ Как делать НЕ НАДО (Антипаттерны)

* **Создавать шлюз ради шлюза** — когда у каждого мелкого одиночного сервиса появляется свой персональный Gateway.
* **Переусложнять цепочки вызовов** — когда для выполнения одного простого действия сетевой трафик вынужден последовательно проходить через три и более шлюзов, накапливая задержки (Network Latency).
* **Создавать хаотичный транзит** — когда внутренние изолированные облака начинают бесконтрольно ходить друг в друга в обход согласованных интерфейсов и периметров безопасности.

##### ✔️ Как делать НАДО (Бест-практики)

* **Привязывать шлюз к контуру** — каждое выделенное логическое облако сервисов имеет строго один свой управляющий Gateway.
* **Опираться на бизнес-логику** — физические границы облаков должны четко отражать реальные границы доменных областей (Bounded Contexts) вашего бизнеса.
* **Соблюдать Конституцию кода** — абсолютно все сетевые пути, права доступа и зависимости между компонентами должны быть явно задекларированы в оркестраторе `AppHost`.
* **Контролировать стыки доменов** — любые межоблачные вызовы (если они необходимы в синхронном режиме) разрешены исключительно через авторизованные шлюзы целевых систем, а не напрямую к внутренним базам или микросервисам.

---

### Итог по архитектуре сети

Ваша фундаментальная инженерная идея полностью жизнеспособна, эффективна и поддерживается платформой из коробки:

$$\text{Один оркестратор Aspire} \Longrightarrow \text{Несколько изолированных Gateway'ов (YARP)}$$

Это надежный и проверенный способ построить предсказуемую, безопасную и легко масштабируемую enterprise-архитектуру, где хаос сетевых коммуникаций исключен на уровне компиляции кода.

### Финальное резюме: Декларативная Федерация Облаков

Ваша фундаментальная инженерная идея не просто жизнеспособна — она описывает зрелую, высококлассную целевую архитектуру для сложных распределенных систем:

$$\text{Один AppHost} \Longrightarrow \text{Несколько шлюзов YARP} \Longrightarrow \text{Несколько логических облаков}$$

#### Ключевые столпы этой концепции

* **Единый центр управления** — вся система оркеструется в рамках одного .NET Aspire проекта.
* **Изоляция на периметре** — каждое логическое облако сервисов имеет свой собственный, строго выделенный Gateway.
* **Контроль топологии** — проект `AppHost` выступает в роли «Конституции», жестко задавая и компилируя сетевые пути.

Внедрение этого подхода превращает стандартную топологию «Hub-and-Spoke» из простого набора шлюзов в её высшую, наиболее зрелую форму: **Декларативную Федерацию Облаков под управлением одного AppHost**. 

Это надежный фундамент, который гарантирует абсолютный порядок в сети, безопасность данных и готовность системы к любым enterprise-нагрузкам.

Мы полностью, красиво и монолитно закрыли огромную главу, посвященную YARP, топологии сети и Декларативной Федерации!

Этот концептуальный переход позволяет приземлить абстрактную идею «Декларативной Федерации» на реальную структуру вашего проекта. Мы переходим от общего списка системных компонентов к их строгой архитектурной классификации.
Ниже представлен готовый фрагмент для продолжения вашего документа в Markdown, оформленный в принятом инженерном и структурированном стиле.

## 3 Доменная декомпозиция и классификация облаков

Переход к многошлюзовой архитектуре требует четкого понимания природы каждого компонента. В текущем списке кандидатов на выделение в собственные «облака» уже отчетливо видны контуры будущих крупных подсистем: `Trading`, `Storage`, `UI`, `Logging`, `Charts`, `BackOffice` и другие.

Однако для предотвращения антипаттерна «гейтвейного спагетти» необходимо внедрить фундаментальное правило проектирования:

$$\text{Компонент системы} \neq \text{Отдельное облако с собственным Gateway}$$

Не каждый кандидат из списка должен обладать собственным сетевым периметром. Некоторые из них являются ключевыми бизнес-доменами, другие — техническими сателлитами, третьи — сквозной инфраструктурой.

### Категоризация системных компонентов

Для принятия осознанных решений о топологии сети все потенциальные кандидаты разделяются на три стратегические категории:

#### 1. Бизнес-облака (Доменные контуры)

Это самостоятельные, крупные контексты (Bounded Contexts), которые содержат ядро бизнес-логики приложения, имеют собственные изолированные хранилища данных и могут развиваться независимыми командами.

* *Примеры из списка:* `Trading` (исполнение ордеров), `BackOffice` (администрирование, клиринг, управление пользователями).
* *Вердикт по Gateway:* **Да, это главные претенденты на выделение собственного шлюза** для изоляции доменных контрактов.

#### 2. Технические сервисы (Сателлиты и функции)

Компоненты, выполняющие специализированную прикладную или визуальную задачу. Они могут быть ресурсоемкими, но не несут в себе центральной транзакционной бизнес-логики.

* *Примеры из списка:* `Charts` (генерация и отрисовка графиков), `Storage` (низкоуровневое дисковое или объектное хранилище).
* *Вердикт по Gateway:* **Опционально.** Чаще всего они прячутся за шлюз соответствующего бизнес-домена или используют единый Edge-периметр.

#### 3. Инфраструктура и сквозные механизмы

Системные компоненты, обеспечивающие жизнедеятельность всего распределенного приложения. Они пронизывают все слои архитектуры и используются каждым микросервисом «из коробки».

* *Примеры из списка:* `Logging` (сбор трейсов, логов, метрик), оркестрация `UI` (фронтенд-хостинг).
* *Вердикт по Gateway:* **Нет, собственный бизнес-шлюз им не нужен.** Инфраструктура вроде логов интегрируется на уровне агентов или сквозного middleware, а UI сам является потребителем шлюзов.

Классификация кандидатов успешно зафиксирована. Мы создали аналитическую базу для детального разбора каждого компонента.

Этот аналитический срез представляет собой готовую прикладную матрицу принятия решений. Перевод в лаконичную и удобную для чтения Markdown-таблицу позволяет визуально сопоставить каждого кандидата со стратегическим решением и его архитектурным обоснованием.

### 3.1 Матрица доменного анализа кандидатов

Для выработки финальной топологии распределенной системы пропустим каждого кандидата через фильтр архитектурной целесообразности. Это позволит отсечь избыточные сетевые периметры на раннем этапе.

| Компонент (Кандидат) | Выделение шлюза / Облака | Архитектурное обоснование и зона ответственности |
| :--- | :---: | :--- |
| **Trading** | **Да** | **Ядро бизнес-логики:** управление ордерами, регистрация сделок, учет позиций, первичный риск-менеджмент и матчинг. |
| **MarketData** | **Да** | **Высоконагруженный домен:** поставка котировок, формирование исторических свечей, трансляция биржевых стаканов и тиковых потоков. |
| **Storage** | **Обычно нет** | **Инфраструктурный слой:** низкоуровневые базы данных, файловые хранилища, распределенный кэш и Blob-контейнеры. Собственный шлюз оправдан только при переходе на изолированный *Storage API*. |
| **UI** | **Нет** | **Клиентская проекция:** фронтенд-приложение является конечным потребителем API, а не серверным доменным облаком. |
| **Logging** | **Нет** | **Платформа наблюдаемости (Observability):** сквозной системный механизм сбора метрик, трейсов и логов. Не содержит бизнес-логики. |
| **Charts** | **Скорее нет (пока)** | **Read-Model домена:** на начальном этапе выступает частью `MarketData` или `Trading`. Выделение в автономный контур требуется только при колоссальной аналитической нагрузке. |
| **BackOffice** | **Да (позже)** | **Операционный домен:** административные панели, клиринг, ручное управление счетами и внутренний аудит. |
| **Risk** | **Да (позже)** | **Бизнес-домен комплаенса:** тяжелые пост-трейд и пре-трейд проверки лимитов, маржинальные вычисления. Требует изоляции от торгового ядра при масштабировании. |
| **Reporting** | **Позже** | **Аналитический слой:** генерация отчетов и тяжелые выгрузки. Может развиваться как часть `BackOffice` или как изолированное OLAP-облако. |
| **Auth / Identity** | **Да (позже)** | **Периметр безопасности:** централизованный провайдер аутентификации и авторизации (IdP) при переходе на реальные промышленные протоколы. |
| **Notifications** | **Позже** | **Шлюз уведомлений:** рассылка Email, Push-сообщений и управление SignalR-соединениями реального времени с клиентами. |

---

### Главный вывод матрицы

На текущем (стартовом) этапе разработки проекта **Trading** вся сложность системы идеально инкапсулируется в **два ключевых бизнес-облака**:

$$\text{Контур Торговли (Trading)} \quad \text{и} \quad \text{Контур Рыночных Данных (MarketData)}$$

Все остальные технические и инфраструктурные кандидаты (такие как `Storage`, `UI`, `Logging`) должны бесшовно потребляться этими облаками напрямую через возможности .NET Aspire, не создавая лишних гейтвейных барьеров. По мере эволюции системы и роста нагрузок из этой матрицы будут последовательно выделяться новые доменные шлюзы (`Auth`, `Risk`, `BackOffice`) по заранее утвержденному плану.

Этот параграф открывает детальный технический разбор первого и самого главного бизнес-контура системы — Trading Cloud. Он наглядно демонстрирует, как абстрактные микросервисы группируются внутри одной физической доменной зоны.
Ниже представлен готовый фрагмент для продолжения вашего документа в Markdown, оформленный в принятом структурном стиле с использованием схем и префиксов.

### 3.2 Детальный разбор целевых контуров

#### 1. Контур Торговли (Trading Cloud) — Первое бизнес-облако

**Trading Cloud** — это главное транзакционное ядро вашей системы. Оно отвечает за жизненный цикл торговых распоряжений, учет клиентского капитала и минимизацию торговых рисков в реальном времени.

##### Архитектурный состав облака

Внутри этого периметра группируется набор высокосвязанных (high-cohesion) микросервисов:

* **Orders API** — прием, валидация и ведение истории ордеров.
* **Execution API** — обработка отчетов об исполнении сделок от бирж и провайдеров ликвидности.
* **Positions API** — расчет текущих открытых позиций и балансов в режиме реального времени.
* **Risk Limits API** — пре-трейд контроль лимитов, маржинальных требований и комплаенс-правил.
* **Matching Adapter** — шлюз сопряжения со сторонними или внутренними мэтчинг-движками.
* **Trading Gateway** — выделенный доменный обратный прокси (YARP), изолирующий эти сервисы от внешнего мира.

##### Топология распределения трафика внутри облака

Trading Cloud (Контур Торговли)
|
+-- trading-gateway (Доменный шлюз YARP)
|
+-- orders-api
+-- execution-api
+-- positions-api
+-- risk-api

##### Стандартизированная карта маршрутов (Префиксы):

Все входящие запросы к этому облаку адресуются через единый глобальный префикс `/api/trading/`, что гарантирует полное отсутствие коллизий на внешнем сетевом периметре:

* `/api/trading/orders` $\rightarrow$ перенаправляется на внутренний сервис **orders-api**
* `/api/trading/positions` $\rightarrow$ перенаправляется на внутренний сервис **positions-api**
* `/api/trading/risk-limits` $\rightarrow$ перенаправляется на внутренний сервис **risk-api**

> **Связь с текущим кодом:**  
> Развернутый в вашей текущей кодовой базе проект `Trading.Api.Orders` является базовым фундаментом и первым легитимным кандидатом на размещение внутри этого изолированного доменного контура.
>

Раздел по первому облаку (Trading Cloud) полностью оформлен и интегрирован в структуру.

Этот параграф детально описывает структуру второго важнейшего контура системы — MarketData Cloud. Он объясняет логику выделения высоконагруженного потока рыночных данных в независимое облако со своей топологией.

#### 2. Контур Рыночных Данных (MarketData Cloud) — Второй бизнес-домен

**MarketData Cloud** — это специализированное высоконагруженное (high-load) облако системы. Его главная задача — обработка, агрегация и трансляция колоссальных объемов рыночной информации с минимальными задержками (low-latency). Выделение этого контура критически важно, чтобы защитить транзакционное ядро торговли от ресурсоемких аналитических вызовов.

##### Архитектурный состав облака1

Внутри этого периметра группируются сервисы, оперирующие потоковыми и историческими биржевыми данными:

* **Quotes API** — обработка текущих цен, спрэдов и мгновенных котировок (BBO).
* **Candles API** — сбор, агрегация и хранение исторических баров (свечей) различных таймфреймов.
* **OrderBook API** — трансляция и поддержка локальных копий биржевых стаканов (глубины рынка).
* **Ticks API** — логирование и стриминг сырого потока обезличенных сделок.
* **MarketData Gateway** — выделенный доменный шлюз YARP, оптимизированный под высокую пропускную способность.

##### Топология распределения трафика внутри облака1

MarketData Cloud (Контур Рыночных Данных)
|
+-- marketdata-gateway (Доменный шлюз YARP)
|
+-- quotes-api
+-- candles-api
+-- orderbook-api

##### Стандартизированная карта маршрутов (Префиксы)

Все запросы на чтение рыночных метрик изолируются под глобальным префиксом `/api/marketdata/`:

* `/api/marketdata/quotes` $\rightarrow$ перенаправляется на внутренний сервис **quotes-api**
* `/api/marketdata/candles` $\rightarrow$ перенаправляется на внутренний сервис **candles-api**
* `/api/marketdata/orderbook` $\rightarrow$ перенаправляется на внутренний сервис **orderbook-api**

> **Связь с текущим кодом:**  
> Ваш текущий проект `Trading.Api.MarketData` — это отправная точка и фундамент для развертывания полноценной инфраструктуры этого автономного облака.

Раздел по второму облаку (MarketData Cloud) успешно сформирован и добавлен в документ.

Этот параграф детально разбирает техническую дихотомию понятия Storage в распределенных системах, четко разделяя инфраструктурный слой данных и прикладные сервисы хранения.
Ниже представлен готовый фрагмент для продолжения вашего документа в Markdown, оформленный в принятом инженерном и структурном стиле с использованием кода конфигурации .NET Aspire.

#### 3. Хранилище данных (Storage) — Инфраструктурный слой или прикладной сервис

При проектировании архитектуры важно четко разграничивать физическую инфраструктуру данных и прикладные бизнес-сервисы. Понятие **Storage** может быть реализовано в двух принципиально разных вариантах.

##### Вариант А. Storage как низкоуровневая инфраструктура (Базовый подход)

Если под Storage подразумеваются стандартные инструменты хранения данных, кэширования или файловые движки:

* **Базы данных:** PostgreSQL, Microsoft SQL Server, MongoDB.
* **Распределенный кэш:** Redis, Memcached.
* **Объектные хранилища / Blob:** MinIO, Amazon S3, Azure Blob Storage.
* **Локальные файловые ресурсы.**

В этом сценарии Storage **не является отдельным облаком с собственным шлюзом YARP**. В экосистеме .NET Aspire это первоклассные инфраструктурные ресурсы (Resources), к которым доменные бэкенды подключаются напрямую.

###### Декларация инфраструктуры в AppHost:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// --- Регистрация инфраструктурных ресурсов ---
var postgres = builder.AddPostgres("postgres");
var tradingDb = postgres.AddDatabase("trading-db");
var cache = builder.AddRedis("cache");
var objectStorage = builder.AddContainer("minio", "minio/minio"); // Эмуляция S3

// --- Подключение ресурсов к бизнес-сервисам напрямую ---
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api")
    .WithReference(tradingDb)
    .WithReference(cache);

var backOfficeApi = builder.AddProject<Projects.BackOffice_Api>("backoffice-api")
    .WithReference(tradingDb)
    .WithReference(objectStorage);
```

Прямые ссылки доменов на инфраструктурные ресурсы абсолютно легитимны. Они контролируются оркестратором, автоматически инжектируют строки подключения (Connection Strings) и не требуют создания гейтвейных барьеров.

---

##### Вариант Б. Storage как прикладной файловый сервис (Продвинутый подход)

Если перед системой встают специализированные прикладные задачи по управлению контентом:

* Генерация, экспорт и длительное хранение тяжелых финансовых отчетов.
* Скачивание юридических документов и договоров.
* Сохранение графических снимков торговых графиков и чартов.
* Управление версионированием пользовательских файлов.

Только в этом случае оправдано выделение сущности в самостоятельный прикладной контур

Storage Cloud (Файловый контур)
|
+-- storage-gateway (Доменный шлюз YARP)
|
+-- storage-api (Микросервис бизнес-логики файлов)

###### Карта маршрутов прикладного хранилища

Вызовы изолируются под глобальным префиксом `/api/storage/`:

* `/api/storage/reports/generate` $\rightarrow$ команда на генерацию отчета.
* `/api/storage/charts/save` $\rightarrow$ сохранение снимка графика.

> **Архитектурный вердикт:**  
> На первом этапе развития вашего проекта Storage должен развиваться исключительно по **Варианту А** (как прямые ссылки на базы данных и кэш). Переходить к **Варианту Б** (выделенному файловому облаку) имеет смысл только при появлении сложной бизнес-логики обработки файлов, чтобы не усложнять топологию преждевременно.

Раздел Storage успешно добавлен и структурирован. Мы зафиксировали важную архитектурную развилку.

Этот параграф вносит предельную ясность в организацию подсистемы наблюдаемости (Observability). Он отсекает опасный антипаттерн создания бизнес-шлюзов для системного трафика и фиксирует нативный подход экосистемы .NET Aspire к сбору телеметрии.
Ниже представлен готовый фрагмент для продолжения вашего документа в Markdown, оформленный в принятом инженерном и структурном стиле.

#### 4. Наблюдаемость (Logging / Observability) — Сквозная инфраструктура

Компонент **Logging** ни в коем случае не должен трансформироваться в классическое бизнес-облако с прикладным шлюзом YARP. Логи, метрики и распределенные трассировки (Traces) представляют собой сквозной технологический слой — **Observability (Наблюдаемость)**, который пронизывает всю систему целиком.

##### Нативные возможности .NET Aspire

В вашей текущей архитектуре этот слой уже развернут и функционирует «из коробки» благодаря интеграции .NET Aspire:

* **OpenTelemetry** — стандартизированный протокол сбора телеметрии.
* **Aspire Dashboard** — встроенная панель для визуализации рантайм-состояния.
* **Structured Logs** — структурированное логирование для глубокой фильтрации.
* **Metrics & Traces** — сквозные трейсы вызовов от фронтенда до баз данных.

##### Целевое расширение стека (Инфраструктурный контур)

По мере роста нагрузок и выхода в промышленную эксплуатацию (Production) этот слой будет расширяться специализированными инструментами. Однако они образуют **инфраструктурный контур**, а не прикладное доменное облако:

Observability Infrastructure (Контур наблюдаемости)
|
+-- OpenTelemetry Collector (Единый приемник)
+-- Seq / Grafana Loki (Хранение и анализ логов)
+-- Prometheus / Grafana (Визуализация метрик)
+-- Elasticsearch / Jaeger (Глубокий анализ трассировок)

##### Архитектурное правило передачи телеметрии

Каждый компонент распределенной системы должен отправлять метрики и логи напрямую в инфраструктурный слой наблюдаемости по стандартным протоколам (OTLP), минуя любые прикладные бизнес-шлюзы:

✔️ ПРАВИЛЬНО (Прямой асинхронный стриминг):
[Trading API] ---- OTLP ----> [OpenTelemetry Collector]
[Gateway]     ---- OTLP ----> [OpenTelemetry Collector]
❌ НЕПРАВИЛЬНО (Гейтвейный барьер):
[Trading API] ---- HTTP ----> [Logging Gateway] ----> [Logging Service]

Внедрение промежуточных бизнес-шлюзов или синхронных REST API для логов приведет к катастрофическому падению производительности системы под нагрузкой.

> **Архитектурный вердикт:**  
> Логирование и мониторинг развиваются параллельно системе как сквозной технический механизм. Выделять для них прикладное облако с YARP-маршрутизацией не нужно ни на старте, ни на продвинутых этапах архитектурной эволюции.

Раздел Logging / Observability успешно оформлен и добавлен в документ.

Этот параграф детально анализирует специфику подсистемы Charts (Графики), разделяя её визуальное представление и логику поставки данных. Он четко аргументирует, почему этот компонент на начальных этапах должен оставаться частью домена рыночных данных.

#### 5. Модуль графиков (Charts) — Сателлит домена рыночных данных

Подсистема **Charts** представляет собой технологически интересную зону, однако выделение её в независимое автономное облако на начальных этапах является преждевременным. Для правильного проектирования этого компонента его необходимо разделить на два независимых слоя:

1. **Визуальный слой (UI-компонент)** — графические библиотеки, чарты и интерактивные элементы на стороне фронтенда (`Trading.Web`).
2. **Слой данных (Data Engine)** — поставка массивов точек, временных рядов и исторических срезов для отрисовки.

##### Логическая привязка к контексту

Данные, необходимые для построения графиков, по своей природе являются фундаментальной частью рыночной информации:

* Исторические бары и свечи (Candles) различных таймфреймов.
* Потоковые обезличенные сделки (Ticks).
* Снимки и срезы биржевых стаканов (OrderBook).
* Объемы торгов (Volumes).

Именно поэтому на старте проекта подсистема графиков естественным образом интегрируется внутрь **MarketData Cloud** в качестве специализированного API-сателлита:

Этот параграф четко разграничивает понятия серверных доменов и клиентских приложений. Он предотвращает серьезную архитектурную ошибку — превращение фронтенда во внутренний сервис — и закладывает основу для использования паттерна BFF (Backend-for-Frontend).
Ниже представлен готовый фрагмент для продолжения вашего документа в Markdown, оформленный в принятом инженерном и структурном стиле.

#### 6. Пользовательские интерфейсы (UI) — Клиентская плоскость системы

Компонент **UI** принципиально не является серверным бизнес-облаком. Это внешняя клиентская плоскость (Presentation Layer), которая служит исключительно для визуализации данных и отправки пользовательских команд в систему.

##### Множественность интерфейсов

В развитой экосистеме распределенного приложения одновременно сосуществуют абсолютно разные типы клиентов:

* **Trading.Web** — основное веб-приложение трейдера (например, на Blazor / Razor).
* **BackOffice.Web / Admin.Web** — внутренние порталы для операционного контроля, комплаенса и администрирования.
* **Mobile / Desktop Apps** — нативные мобильные и настольные терминалы.

Все эти приложения являются внешними **потребителями API**, а не внутренними микросервисами распределенной сети. Они физически вынесены за защищенный внутренний контур.

##### Топология интеграции UI через паттерн BFF / Edge Gateway

Интерфейсы никогда не должны обладать собственными «облачными шлюзами» в том же смысле, что и бэкенд-домены. Вместо этого они взаимодействуют с системой через зонтичный Edge Gateway или специализированные шлюзы для фронтенда — **BFF (Backend-for-Frontend)**:

[ Trading.Web ]      [ BackOffice.Web ]
|                     |
+----------+----------+
|
v
[ Edge Gateway ] (Единый внешний периметр)
|
+--------+--------+
|                 |
v                 v
Trading Cloud     BackOffice Cloud

##### Архитектурная эволюция: Переход к выделенным BFF

При усложнении логики отображения и разграничении прав доступа единый Edge Gateway эволюционирует в цепочку изолированных BFF-шлюзов. Каждый тип UI получает свою персональную точку входа, оптимизированную под его конкретные нужды (склейка DTO, агрегация данных, сессионная авторизация):

* **Trading.Web** $\rightarrow$ взаимодействует исключительно с **Trading BFF** (шлюз для трейдеров).
* **BackOffice.Web** $\rightarrow$ взаимодействует исключительно с **BackOffice BFF** (административный шлюз).
* **Mobile App** $\rightarrow$ взаимодействует исключительно с **Mobile BFF** (оптимизация мобильного трафика).

> **Архитектурный вердикт:**  
> UI — это клиентская проекция системы. Он не создает вокруг себя серверного облака. Управление трафиком от интерфейсов реализуется через паттерны **Edge Gateway** или **BFF**, которые переводят внешние запросы клиентов на жесткий рельсовый путь внутренней Декларативной Федерации Облаков.

Раздел UI и клиентская плоскость успешно переведен в Markdown и интегрирован в структуру. Наша аналитическая база полностью сформирована.

Этот параграф детально описывает структуру третьего важнейшего бизнес-контура системы — BackOffice Cloud. Он фиксирует правила изоляции административного домена и закладывает строгие архитектурные регламенты взаимодействия между бэкенд-облаками.

#### 7. Контур Бэк-офиса (BackOffice Cloud) — Административный бизнес-домен

**BackOffice Cloud** — это полноценный, независимый операционный и административный бизнес-домен системы. Он предназначен для сотрудников компании, комплаенс-контролеров, риск-менеджеров и администраторов. Выделение этого контура в изолированное облако позволяет полностью отсечь тяжелый регуляторный и отчетный трафик от транзакционного ядра торговли.

##### Архитектурный состав облака2

Внутри этого периметра группируются сервисы, обеспечивающие операционную деятельность и аудит:

* **Admin API** — управление пользователями, правами доступа (РБАК/роль-модель) и верификация клиентов (KYC).
* **Instruments API** — конфигурация торговых инструментов, спецификаций контрактов, расписаний сессий и торговых плеч.
* **Reports API** — генерация регуляторной отчетности, брокерских отчетов и выгрузок.
* **Audit API** — сквозное ведение операционных журналов и неизменяемый лог действий сотрудников.

##### Топология распределения трафика внутри облака2





