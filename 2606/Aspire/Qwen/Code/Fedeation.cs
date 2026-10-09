Aspire, Projects, Gateway, Http, Interaction
https://chat.qwen.ai/s/75eecb49-01e6-446f-8953-2f35f9d9d9b8?fev=0.3.12
https://gsqwen.blogspot.com/2026/09/aspire-projects.html
Давайте порассуждаем по развитии этой системы Облаков. Можно даже назвать каждое Облако - Bounded Context. МЫ научились создавать Bounded Context-s в контексте Aspire. Тогда по аналогии с DDD надо наладить между Bounded Context Взаимодействие. Как Вы видете реализацию Взаимодействия между Bounded Context-ами. Напоминаю, что пока наши Облака - Bounded Context-s пока еще сидят в контексте Aspire ?
Да, это очень правильный поворот мысли.
Если каждое «Облако» вы начинаете рассматривать как Bounded Context, то главный вопрос становится уже не «как поднять ещё один сервис», а:
Как два ограниченных контекста могут взаимодействовать, не разрушая свои границы?
И то, что пока все эти Bounded Contexts живут внутри одного Aspire — это не слабость, а наоборот, отличная стартовая позиция. У вас есть единый контрольный контур:
Aspire AppHost | +-- Bounded Context A +-- Bounded Context B +-- Bounded Context C
То есть вы можете отработать DDD-интеграцию локально, декларативно и наблюдаемо, не вынося сразу систему во внешний кластер.
________________________________________
Главная идея
Я бы строил взаимодействие между Bounded Context-ами по такому принципу:
Внутри контекста сервисы могут быть любыми.
Снаружи контекст имеет только одну публичную границу — свой Gateway.
Взаимодействие между контекстами происходит только через их Gateway или через явный интеграционный механизм, объявленный в AppHost.
То есть:
Bounded Context A internal services internal databases internal domain model | v Context A Gateway <----> Context B Gateway
А не так:
Context A Service -> Context B Service Context A Repository -> Context B Database Context B Worker -> Context A internal API
Иначе Bounded Context перестаёт быть bounded.
1. Bounded Context в вашей модели
Каждое Облако / Bounded Context может состоять из нескольких Aspire-проектов:
Trading Bounded Context | +-- trading-gateway +-- orders-api +-- positions-api +-- risk-api
Но внешний мир знает только:
trading-gateway
Внутренние сервисы контекста не должны быть видимы другим контекстам напрямую.
Это очень хорошо ложится на Aspire:
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api"); var positionsApi = builder.AddProject<Projects.Trading_Api_Positions>("positions-api"); var tradingGateway = builder.AddProject<Projects.Trading_Gateway>("trading-gateway") .WithReference(ordersApi) .WithReference(positionsApi);
Другие контексты ссылаются не на orders-api, а на trading-gateway.
2. Три базовых типа взаимодействия между Bounded Context-ами
В DDD-системах взаимодействие между контекстами почти всегда распадается на три категории:
1. Запрос данных 2. Выполнение команды 3. Публикация доменного события
Их нельзя смешивать, потому что у них разные требования.
Тип 1. Запрос данных
Например:
Trading Context хочет узнать текущую цену инструмента из MarketData Context.
или
BackOffice Context хочет получить список ордеров из Trading Context.
Это запрос на чтение.
Для него лучше всего подходит синхронный HTTP:
Trading Gateway | | GET /api/marketdata/quotes/AAPL v MarketData Gateway
Характеристики:
•	синхронно;
•	идемпотентно;
•	можно кэшировать;
•	можно повторять;
•	легко трассировать;
•	легко отлаживать.
В Aspire это выражается просто:
var tradingGateway = builder.AddProject<Projects.Trading_Gateway>("trading-gateway") .WithReference(marketDataGateway);
То есть Trading Context получает привилегию обращаться к MarketData Context.
Если этой ссылки нет — обращения быть не должно.
________________________________________
Тип 2. Команда
Например:
BackOffice Context хочет заблокировать инструмент. Risk Context хочет снизить лимит. Trading Context хочет принять ордер.
Команда — это намерение изменить состояние.
Она должна быть:
•	явно адресована;
•	идемпотентна;
•	авторизована;
•	проверена;
•	принята или отклонена.
Пример:
BackOffice Gateway | | POST /api/trading/v1/orders/{id}/cancel v Trading Gateway
Или:
Risk Gateway | | POST /api/trading/v1/risk-limits v Trading Gateway
Для команд я бы тоже начинал с HTTP.
Если команда быстрая и требует немедленного результата:
POST /api/trading/v1/orders -> 201 Created
И дальше контекст уже публикует события:
OrderAccepted
OrderRejected
OrderExecuted
Тип 3. Доменное событие
Например:
Trading Context: OrderPlaced OrderAccepted OrderRejected TradeExecuted MarketData Context: QuoteUpdated CandleClosed BackOffice Context: ReportGenerated
Событие — это факт, который уже произошёл.
Событие не должно спрашивать разрешение.
Оно сообщает:
Вот что случилось. Кому нужно — отреагирует.
Для событий синхронный HTTP — не лучший основной механизм.
Здесь нужен асинхронный подход:
Publisher Context | | сохраняет событие в Outbox v Dispatcher | | HTTP / EventExchange / PubSub v Subscriber Context
3. Как я вижу основной механизм взаимодействия внутри Aspire
Пока контексты живут в одном Aspire, я бы использовал такую модель:
Синхронные запросы и команды: Gateway -> Gateway по HTTP Асинхронные события: Gateway/Context -> Outbox -> HTTP-доставка подписчикам или позже: Gateway/Context -> Outbox -> EventExchange -> подписчики
То есть на первом этапе:
HTTP + Outbox
А не:
сразу RabbitMQ / Kafka / Dapr / Wolverine
Это соответствует вашей линии:
Сначала простой, понятный, наблюдаемый HTTP.
Сложность добавляем только тогда, когда она действительно нужна.
4. Синхронное взаимодействие через Context Gateway
Пример контекстов:
Trading Context MarketData Context BackOffice Context
Допустим:
Trading Context имеет право читать котировки из MarketData Context. BackOffice Context имеет право читать ордера из Trading Context.
В AppHost:
var builder = DistributedApplication.CreateBuilder(args); // Trading Context var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api"); var tradingGateway = builder.AddProject<Projects.Trading_Gateway>("trading-gateway") .WithReference(ordersApi); // MarketData Context var marketDataApi = builder.AddProject<Projects.MarketData_Api>("market-data-api"); var marketDataGateway = builder.AddProject<Projects.MarketData_Gateway>("marketdata-gateway") .WithReference(marketDataApi); // BackOffice Context var reportsApi = builder.AddProject<Projects.BackOffice_Api_Reports>("reports-api"); var backOfficeGateway = builder.AddProject<Projects.BackOffice_Gateway>("backoffice-gateway") .WithReference(reportsApi); // Context Map: разрешённые межконтекстные связи tradingGateway .WithReference(marketDataGateway); backOfficeGateway .WithReference(tradingGateway);
Это уже не просто инфраструктура.
Это уже контекстная карта, выраженная в коде.
________________________________________
5. Антикоррупционный слой между контекстами
Очень важно: один контекст не должен напрямую использовать внутренние модели другого контекста.
Например, Trading Context не должен говорить:
var marketDataDto = await httpClient.GetFromJsonAsync<MarketDataDto>(...); domainService.Use(marketDataDto);
Потому что MarketDataDto — это язык чужого контекста.
Правильнее:
public interface IMarketQuotesPort { Task<Quote?> GetQuoteAsync(string symbol, CancellationToken ct = default); }
И адаптер:
public sealed class MarketDataGatewayClient : IMarketQuotesPort { private readonly HttpClient _http; public MarketDataGatewayClient(HttpClient http) { _http = http; } public async Task<Quote?> GetQuoteAsync(string symbol, CancellationToken ct = default) { var dto = await _http.GetFromJsonAsync<MarketDataDto>( $"/api/marketdata/{symbol}", ct); if (dto is null) return null; return new Quote(dto.Symbol, dto.Price, dto.Timestamp); } }
То есть:
Чужой DTO не проникает в домен. Контекст использует свой порт и свой адаптер.
Это и есть Anti-Corruption Layer из DDD.
6. Published Language: общий язык интеграции
Между Bounded Context-ами должен существовать отдельный контрактный язык.
Не внутренний доменный язык.
Не сущности базы данных.
Не агрегаты.
А специальный интеграционный язык:
Integration Commands Integration Events Integration DTOs
Например:
public record OrderPlacedV1( Guid OrderId, string Symbol, string Side, decimal Price, int Quantity, DateTime OccurredAtUtc);
Или:
public record TradeExecutedV1( Guid TradeId, Guid OrderId, string Symbol, decimal Price, int Quantity, DateTime ExecutedAtUtc);
Эти контракты нужно выносить отдельно.
Например:
Trading.IntegrationContracts MarketData.IntegrationContracts BackOffice.IntegrationContracts1. Создать ордер. 2. Записать событие OrderPlacedV1 в Outbox. 3. Зафиксировать транзакцию. 4. Вернуть 202 Accepted.
Или, если хочется проще на старте:
Trading.Contracts.Integration
Но важно:
Интеграционные контракты не должны содержать богатую доменную модель контекста.
Они должны содержать только то, что контекст согласен публиковать наружу.
7. Асинхронные события: Outbox как первый этап Для событий я бы начал с простого, но надежного механизма:
Контекст сохраняет событие в таблицу Outbox Фоновый диспетчер доставляет событие подписчикам Подписчик подтверждает обработку
Например, Trading Context принимает ордер:
POST /api/trading/v1/orders
Внутри одной транзакции:
1. Создать ордер.
2. Записать событие OrderPlacedV1 в Outbox.
3. Зафиксировать транзакцию.
4. Вернуть 202 Accepted.
Потом фоновый диспетчер доставляет событие:
Trading Outbox | v BackOffice Gateway /integration/events Risk Gateway /integration/events MarketData Gateway /integration/events
Это даёт вам:
•	гарантию, что событие не потеряется;
•	независимость от доступности подписчиков;
•	возможность повторной доставки;
•	простую диагностику;
•	отсутствие немедленной необходимости в брокере.
8. Как может выглядеть событие
Я бы использовал конверт события.
Например:
{ "eventId": "7c1e6f2e-9d1b-4f6c-9a2b-6f2d1e0c9a11", "eventType": "trading.order-placed.v1", "sourceContext": "trading", "occurredAtUtc": "2026-01-01T10:00:00Z", "correlationId": "d2f7c1e6-5b4a-4f3e-9a2b-6f2d1e0c9a77", "data": { "orderId": "1b1e0c9a-6f2d-4f6c-9a2b-7c1e6f2e9d11", "symbol": "AAPL", "side": "Buy", "price": 175.50, "quantity": 10 } }
Или можно ориентироваться на формат CloudEvents.
Главное, чтобы у события были:
eventId eventType sourceContext occurredAtUtc correlationId data
eventId нужен для идемпотентности.
Подписчик должен уметь сказать:
Я уже обработал событие с таким eventId. Повторно обрабатывать не буду.
9. Куда доставлять события: прямой вызов или EventExchange
Здесь есть два уровня зрелости.
________________________________________
Уровень 1. Прямая доставка подписчикам
Издатель знает подписчиков и отправляет событие им по HTTP.
Trading Context | +---> BackOffice Gateway +---> Risk Gateway +---> MarketData Gateway
Плюсы:
•	просто;
•	понятно;
•	легко трассировать;
•	не нужен отдельный инфраструктурный сервис.
Минусы:
•	издатель знает подписчиков;
•	при добавлении нового подписчика нужно менять издателя;
•	появляется риск жесткой связности.
Для двух-трех контекстов это нормально.
________________________________________
Уровень 2. Отдельный Integration Exchange
Когда подписчиков становится больше, можно ввести отдельный интеграционный узел:
Trading Context
    |
    v
Integration Exchange
    |
    +---> BackOffice Context
    +---> Risk Context
    +---> MarketData Context
    +---> Reporting Context
Это тоже может быть Aspire-проектом:
var exchange = builder.AddProject<Projects.Integration_Exchange>("integration-exchange");
Тогда:
Издатель знает только обмен. Обмен знает маршруты подписок. Подписчики знают только свой входящий контракт.
Это уже похоже на легковесный событийный хаб внутри Aspire.
Но важно:
Не нужно строить этот обмен заранее.
Он нужен, когда появляется реальный fan-out: одно событие интересует многим контекстам.


10. Как это может выглядеть в AppHost
Если вы вводите Integration Exchange, топология может быть такой:
var builder = DistributedApplication.CreateBuilder(args); // Контексты var tradingGateway = builder.AddProject<Projects.Trading_Gateway>("trading-gateway"); var marketDataGateway = builder.AddProject<Projects.MarketData_Gateway>("marketdata-gateway"); var backOfficeGateway = builder.AddProject<Projects.BackOffice_Gateway>("backoffice-gateway"); // Интеграционный обмен var exchange = builder.AddProject<Projects.Integration_Exchange>("integration-exchange") .WithReference(tradingGateway) .WithReference(marketDataGateway) .WithReference(backOfficeGateway); // Контексты имеют право публиковать/получать через обмен tradingGateway.WithReference(exchange); marketDataGateway.WithReference(exchange); backOfficeGateway.WithReference(exchange);
Здесь integration-exchange становится инфраструктурным контекстом.
Но он должен быть максимально «тупым»:
принял событие; сохранил; маршрутизировал; повторил при ошибке; показал диагностику.
 Он не должен знать бизнес-правила.
________________________________________
11. Что лучше: Gateway-to-Gateway или общий обмен?
Я бы разделил так:
Тип взаимодействия	Механизм
Запрос данных	Gateway-to-Gateway HTTP
Быстрая команда	Gateway-to-Gateway HTTP
Долгая команда	202 Accepted + события
Факт, который случился	Integration Event через Outbox
Одно событие, много подписчиков	EventExchange
Реалтайм для UI	SignalR через Edge Gateway
Тяжелая асинхронная федерация	позже Wolverine / NATS / RabbitMQ
То есть не нужно выбирать один механизм на все случаи жизни.
________________________________________
12. Очень важный DDD-принцип: контексты не должны делить внутреннее состояние
Частая ошибка:
Trading и BackOffice используют одну и ту же таблицу Orders.
Или:
Risk напрямую читает внутреннюю базу Trading.
Это разрушает границу.
Правильнее:
Trading Context владеет своими данными. Другие контексты получают либо запрос к Trading Gateway, либо события от Trading Context.
Например:
BackOffice не читает таблицу Trading. BackOffice подписывается на: OrderPlaced OrderAccepted TradeExecuted и строит свою проекцию для отчетов.
Это уже элементы CQRS и событийной интеграции.
________________________________________
13. Пример: интеграция Trading и BackOffice
Допустим:
Пользователь размещает ордер в Trading. BackOffice должен увидеть этот ордер в отчетах.
Плохо:
BackOffice напрямую читает базу Trading.
Хорошо:
1. Trading принимает ордер. 2. Trading сохраняет ордер. 3. Trading сохраняет событие OrderPlacedV1 в Outbox. 4. Trading отвечает клиенту 202 Accepted. 5. Outbox dispatcher доставляет событие в BackOffice. 6. BackOffice сохраняет проекцию заказа в своей базе. 7. Отчеты читают уже проекцию BackOffice.
Схема:
Trading Context | | OrderPlacedV1 v BackOffice Context | v BackOffice Read Model
Это дает независимость:
Если BackOffice упадет, Trading продолжит принимать ордера. Когда BackOffice поднимется, события будут доставлены повторно.
14. Пример: интеграция Trading и MarketData
Допустим, Trading Context хочет проверить цену перед принятием ордера.
Здесь возможны два варианта.
________________________________________
Вариант 1. Синхронный запрос
Trading Gateway | | GET /api/marketdata/v1/quotes/AAPL v MarketData Gateway
Подходит, если:
•	цена нужна прямо сейчас;
•	запрос быстрый;
•	отказ можно обработать явно.
________________________________________
Вариант 2. Локальная проекция цен
Если цена нужна часто, Trading Context может подписаться на:
QuoteUpdated
и хранить последнюю цену у себя.
Тогда:
MarketData Context публикует QuoteUpdated. Trading Context обновляет локальную таблицу/кэш последних цен. Trading использует локальную копию для быстрых проверок.
Это более зрелая модель, потому что уменьшает синхронную зависимость.
15. Роль Edge Gateway в этой картине
Если у вас есть пользовательские интерфейсы:
Trading.Web BackOffice.Web Admin.Web
я бы не позволял им напрямую ходить во все контексты.
Лучше:
Trading.Web | v Edge Gateway | +-- Trading Gateway +-- MarketData Gateway +-- BackOffice Gateway
Тогда UI знает только один вход.
А межконтекстное взаимодействие всё равно остается между шлюзами контекстов:
Trading Gateway <-> MarketData Gateway Trading Gateway -> BackOffice Gateway BackOffice Gateway -> Trading Gateway
16. Как связать это с вашим текущим кодом
Сейчас у вас есть:
Trading.Web | v Trading.Gateway | +--> Trading.Api.Orders +--> Trading.Api.MarketData
Пока это один общий Gateway.
На пути к Bounded Contexts я бы эволюционировал его так:
Этап 1
Оставить один Gateway, но внутри маршруты уже разделить:
/api/trading/* /api/marketdata/*
Этап 2
Выделить два Gateway:
Trading.Gateway MarketData.Gateway
Этап 3
Добавить:
Edge.Gateway
или превратить текущий Trading.Gateway в Edge.Gateway.
________________________________________
Этап 4
Когда появится BackOffice, добавить:
BackOffice.Gateway
Этап 5
Когда появятся реальные события, добавить:
Outbox IntegrationEvents EventExchange при необходимости
17. Что я бы сделал с контрактами
Для текущей системы можно начать с простого проекта:
Trading.Integration
И туда положить не внутренние сущности, а интеграционные сообщения.
Например:
namespace Trading.Integration.Events; public record OrderPlacedV1 { public Guid EventId { get; init; } = Guid.NewGuid(); public Guid OrderId { get; init; } public string Symbol { get; init; } = string.Empty; public string Side { get; init; } = "Buy"; public decimal Price { get; init; } public int Quantity { get; init; } public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow; }
namespace Trading.Integration.Events; public record TradeExecutedV1 { public Guid EventId { get; init; } = Guid.NewGuid(); public Guid TradeId { get; init; } public Guid OrderId { get; init; } public string Symbol { get; init; } = string.Empty; public decimal Price { get; init; } public int Quantity { get; init; } public DateTime ExecutedAtUtc { get; init; } = DateTime.UtcNow; }
И отдельно:
namespace Trading.Integration.Commands; public record PlaceOrderV1 { public string Symbol { get; init; } = string.Empty; public string Side { get; init; } = "Buy"; public decimal Price { get; init; } public int Quantity { get; init; } }
Это уже опубликованный язык контекста.
________________________________________
18. Что я бы не делал
Я бы не делал следующее:
1. Не давал бы контекстам прямой доступ к внутренним сервисам друг друга
Плохо:
builder.AddProject<Projects.BackOffice_Api_Reports>("backoffice-reports") .WithReference(ordersApi);
Правильно:
builder.AddProject<Projects.BackOffice_Api_Reports>("backoffice-reports") .WithReference(tradingGateway);
2. Не строил бы сразу событийную шину ради самой шины
Не нужно сразу тащить:
RabbitMQ Kafka NATS Dapr PubSub Wolverine
если у вас пока:
два контекста; три события; один пользовательский сценарий.
Сначала можно сделать:
Outbox + HTTP dispatch
3. Не использовал бы события для запросов
Событие — это факт.
Если вам нужен ответ:
Дай мне цену
это не событие.
Это запрос.
Для запроса лучше использовать:
HTTP GET
или gRPC, если понадобится.
________________________________________
4. Не делал бы распределённые транзакции
Не нужно пытаться сделать:
Trading, Risk и BackOffice должны закоммититься одновременно.
В распределенной системе это дорого и хрупко.
Лучше:
Локальная транзакция в контексте + Событие + Идемпотентная обработка + Компенсирующее действие при ошибке
19. Как это связано с DDD Context Mapping
В терминах DDD ваша архитектура может использовать несколько паттернов.
________________________________________
Published Language
Каждый контекст публикует понятные контракты:
OrderPlacedV1 TradeExecutedV1 QuoteUpdatedV1
Open Host Service
Каждый контекст имеет публичную точку входа:
Context Gateway
Anti-Corruption Layer
Потребитель оборачивает чужой контракт своим портом:
IMarketQuotesPort ITradingOrdersPort IRiskLimitsPort
Customer / Supplier
Например:
Trading Context является поставщиком данных для BackOffice Context.
Но поставка должна идти через опубликованный язык, а не через внутреннюю базу.
________________________________________
20. Моя итоговая формула
Если сформулировать максимально коротко:
Каждый Bounded Context в вашем Aspire-пространстве должен иметь собственный Gateway как единственную публичную границу.
Синхронные запросы и команды между контекстами идут через HTTP Gateway-to-Gateway и разрешаются в AppHost через WithReference.
Асинхронные факты домена передаются как Integration Events через Outbox, а при росте числа подписчиков — через отдельный Integration Exchange, тоже объявленный в Aspire.
Или еще короче:
Bounded Context = Gateway + internal services. Context-to-Context Sync = HTTP via Gateway. Context-to-Context Async = Integration Event + Outbox. Topology = AppHost WithReference. Contracts = Published Language, not internal domain model.
21. Что я бы сделал прямо сейчас на вашем месте
Для текущего проекта я бы двигался так:
Шаг 1. Четко разделить контексты по маршрутам
Сейчас:
/api/orders /api/marketdata
Лучше:
/api/trading/orders /api/marketdata/quotes
Это подготовит будущие границы.
________________________________________
Шаг 2. Ввести интеграционные контракты
Создать:
Trading.Integration
и описать:
PlaceOrderV1 OrderPlacedV1 QuoteUpdatedV1
Шаг 3. Сделать Gateway не только прокси, а первым фасадом
Например:
GET /api/trading/dashboard/{symbol}
который агрегирует:
Trading Context MarketData Context
Шаг 4. Когда появятся мутации, добавить простой Outbox
Не сразу брокер.
А сначала:
OutboxMessages BackgroundDispatcher HTTP delivery Idempotency-Key / EventId
Шаг 5. Только потом думать о EventExchange или Wolverine
Когда появится:
много подписчиков; много событий; требования к доставке; необходимость масштабирования; потребность в ретраях и дедупликации на уровне платформы.
Финальный вывод
Вы абсолютно правы: если Облако — это Bounded Context, то дальше нужно проектировать не «сервисы», а границы и языки взаимодействия.
Внутри Aspire это можно сделать очень элегантно:
AppHost задает карту контекстов. WithReference задает разрешенные связи. Каждый контекст имеет свой Gateway. Синхронное взаимодействие — через Gateway по HTTP. Асинхронное — через события и Outbox. При росте — появляется внутренний EventExchange.
----- 
И самое важное:
Пока контексты живут в одном Aspire — это идеальный полигон.
Вы можете отработать DDD-интеграцию без внешней инфраструктурной боли.
А когда система вырастет, те же контракты, границы и маршруты можно будет перенести уже на более тяжелый транспорт.
Большое спасибо за Ваше мнение. Чем дальше идем , тем становится Интереснее и интереснее. Ну что же, допустим что мы сумели реализовать наши Облачные Bounded Contexts. Что же дальше ? А дальше нам остается придумать и реализовать Взимоджействие наших Bounded Contexts уже с RAbbit, Kafka, Redis итд. То есть мы хотим провзаимодействовать с другими Aspire. Это будет уже конечным этапом нашей мыслей и наших действий. ---------------------- Я бы для этого выделил в Среде Aspire отдельную, выделенную Сущность специально для этих Операций. Там я бы сделал возможность расставлять Розетки для взаимодйствия с разными внешними сущностями RAbbit, Kafka, Redis Что думаете ВЫ
-------------- 
Идея превосходная, и она логически завершает вашу архитектурную эволюцию. Вы интуитивно пришли к тому, что в терминологии гексагональной архитектуры называется порты и адаптеры, но применённо к целой федерации контекстов.
Позвольте развить мысль.
________________________________________
1. Почему это неизбежный следующий шаг
Вспомните путь, который вы уже прошли:
Шаг 1: Хаос прямых связей сервисов -> решили через центральный Router 
Шаг 2: Хаос прямых связей между Bounded Contexts -> решили через Context Gateway + события 
Шаг 3: Хаос прямых связей с внешним миром -> ???
Если каждый Bounded Context будет сам подключаться к внешнему Rabbit, внешней Kafka или чужому Aspire, вы получите новое спагетти, только уже на границе системы:
Trading Context -> внешний Rabbit BackOffice Context -> внешний Kafka MarketData Context -> внешний Redis
Risk Context -> чужой Aspire API
Это ровно та же проблема N × N, которую вы уже решили внутри. Только теперь N внешних систем умножается на N внутренних контекстов.
Поэтому ответ однозначен:
Нужен внешний периметр федерации. Отдельная сущность, которая является единственной границей между вашим внутренним миром и внешним.
2. Как я бы назвал эту сущность
Есть несколько удачных терминов:
•	Integration Gateway
•	External Edge
•	Federated Ports Hub
•	Boundary Gateway
Но мне больше всего нравится метафора, которую вы сами подсказали:
Розеточная панель федерации
Потому что суть именно такая:
Внешний мир имеет много разных "вилок": 
Rabbit-вилка Kafka-вилка Redis-вилка HTTP-вилка gRPC-вилка чужой Aspire-вилка Ваша розеточная панель даёт под каждую вилку свой стандартизированный разъём, а внутри федерации всё уже ходит по единому закону.
3. Архитектурная схема
Полная картина выглядит так:
                         ВНЕШНИЙ МИР | +------------------------+------------------------+ | | | внешний Rabbit внешний Kafka чужой Aspire | | | +------------------------+------------------------+ | [ РОЗЕТОЧНАЯ ПАНЕЛЬ ] Integration Gateway | +------------------------+------------------------+ | | | v v v Trading Context MarketData Context BackOffice Context (trading-gateway) (marketdata-gateway) (backoffice-gateway) | | | +------------------------+------------------------+ | [ ВНУТРЕННИЙ МИР ] под управлением AppHost
То есть у вас появляется два уровня шлюзов:


Уровень	Назначение
Внутренний шлюз контекста	граница Bounded Context
Внешний интеграционный шлюз	граница всей федерации
И это очень красиво согласуется с вашим принципом:
Каждая граница должна быть явной, наблюдаемой и управляемой.
4. Что делает розеточная панель
У неё три обязанности.
________________________________________
Обязанность 1. Входящая адаптация
Внешний мир присылает события в своём формате:
внешний 
Rabbit: message from legacy billing system внешний 
Kafka: market feed event чужой Aspire: HTTP callback
Розеточная панель:
•	принимает сообщение;
•	проверяет его;
•	преобразует во внутренний интеграционный контракт;
•	передаёт внутрь федерации через внутренний механизм событий.
То есть:
External Event -> Adapter -> Internal Integration Event -> нужный Bounded Context
Обязанность 2. Исходящая адаптация
Внутренний контекст публикует событие:
OrderExecutedV1
Розеточная панель решает:
•	нужно ли это событие отправить наружу;
•	в какую внешнюю систему;
•	в каком формате;
•	с какими гарантиями.
Например:
OrderExecutedV1 -> Kafka topic trades -> Rabbit queue billing -> HTTP webhook partner
Обязанность 3. Изоляция внешнего хаоса
Внешний мир нестабилен:
•	внешние системы падают;
•	меняют контракты;
•	присылают дубли;
•	присылают мусор;
•	имеют другие форматы времени, валют, идентификаторов.
Розеточная панель — это антикоррупционный барьер на границе федерации.
Внутренние контексты никогда не должны видеть:
сырые сообщения внешнего Rabbit; бинарные ключи внешней Kafka; структуры данных чужого Aspire.
Они видят только чистые внутренние контракты.
________________________________________
5. Розетки как паттерн адаптеров
Ваша идея про розетки очень точно ложится на код.
Можно определить общий интерфейс:
public interface IExternalConnector { string ConnectorId { get; } Task StartAsync(CancellationToken ct); 
Task StopAsync(CancellationToken ct); // Входящее сообщение из внешнего мира IAsyncEnumerable<ExternalMessage> ReceiveAsync(CancellationToken ct); // Исходящее сообщение во внешний мир Task SendAsync(ExternalMessage message, CancellationToken ct); }
И конкретные розетки:
public sealed class RabbitMqConnector : IExternalConnector { public string ConnectorId => "external-rabbit"; } public sealed class KafkaConnector : IExternalConnector { public string ConnectorId => "external-kafka"; } public sealed class RedisConnector : IExternalConnector { public string ConnectorId => "external-redis"; } public sealed class ExternalAspireConnector : IExternalConnector { public string ConnectorId => "partner-aspire"; }
А розеточная панель управляет ими как коллекцией:
public sealed class IntegrationPanel { private readonly IReadOnlyCollection<IExternalConnector> _connectors; public async Task RouteInboundAsync(ExternalMessage message) { // адаптация и передача внутрь федерации } public async Task RouteOutboundAsync(InternalEvent evt) { // выбор внешних адресатов } }
Это и есть ваша розеточная панель.
________________________________________
6. Как это объявить в Aspire
В AppHost это может выглядеть примерно так:
var builder = DistributedApplication.CreateBuilder(args); // Внутренние контексты var tradingGateway = builder.AddProject<Projects.Trading_Gateway>("trading-gateway"); var marketDataGateway = builder.AddProject<Projects.MarketData_Gateway>("marketdata-gateway"); var backOfficeGateway = builder.AddProject<Projects.BackOffice_Gateway>("backoffice-gateway"); // Внешние инфраструктуры var externalRabbit = builder.AddRabbitMQ("external-rabbit"); var externalKafka = builder.AddKafka("external-kafka"); var externalRedis = builder.AddRedis("external-redis"); // Розеточная панель var integrationPanel = builder.AddProject<Projects.Integration_Panel>("integration-panel") .WithReference(externalRabbit) .WithReference(externalKafka) .WithReference(externalRedis) .WithReference(tradingGateway) .WithReference(marketDataGateway) .WithReference(backOfficeGateway); // Важно: // ни один бизнес-контекст НЕ имеет прямой ссылки на внешний Rabbit, Kafka или Redis
Это критически важно:
Только розеточная панель знает, что внешний мир существует.
Бизнес-контексты живут в чистом внутреннем мире.
Это прямое продолжение вашего принципа:
Безопасность через физическое отсутствие путей.
Если у Trading Context нет WithReference(externalRabbit), он физически не может туда подключиться.
________________________________________
7. Манифест внешних подключений
Ваша идея про розетки естественным образом требует манифеста внешних связей.
Например, в коде:
public static class ExternalTopology { public static readonly ExternalLink BillingRabbit = new() { Connector = "external-rabbit", Direction = LinkDirection.Inbound, ExternalEntity = "legacy-billing", InternalTargetContext = "trading", InternalEventType = "InvoiceReceivedV1" }; public static readonly ExternalLink MarketFeedKafka = new() { Connector = "external-kafka", Direction = LinkDirection.Inbound, ExternalEntity = "market-data-vendor", InternalTargetContext = "marketdata", InternalEventType = "QuoteUpdatedV1" }; public static readonly ExternalLink PartnerWebhook = new() { Connector = "partner-aspire", Direction = LinkDirection.Outbound, ExternalEntity = "partner-clearing", InternalEventType = "TradeExecutedV1" }; }
Или в конфигурации:
{ "ExternalConnections": [ { "connector": "external-rabbit", "direction": "inbound", "externalEntity": "legacy-billing", "internalContext": "trading", "internalEventType": "InvoiceReceivedV1" }, { "connector": "external-kafka", "direction": "inbound", "externalEntity": "market-data-vendor", "internalContext": "marketdata", "internalEventType": "QuoteUpdatedV1" }, { "connector": "partner-aspire", "direction": "outbound", "externalEntity": "partner-clearing", "internalEventType": "TradeExecutedV1" } ] }
Тогда у вас появляется единый реестр всех внешних связей федерации.
Это уже не просто код. Это дипломатический паспорт вашей системы: с кем она общается, через какие розетки и по каким договорам.
________________________________________
8. Что должно быть внутри каждой розетки
Каждая розетка должна решать набор типовых задач.
________________________________________
1. Форматная адаптация
Внешний мир никогда не говорит на вашем языке.
Например:
Внешний формат: { "msg_id": "123", "ts": 1710000000, "sym": "AAPL", "px": "175.50" } Внутренний формат: { "eventId": "...", "eventType": "quote-updated.v1", "occurredAtUtc": "...", "data": { "symbol": "AAPL", "price": 175.50 } }
Розетка делает преобразование.
________________________________________
2. Идемпотентность
Внешние системы часто шлют дубли.
Каждая розетка должна уметь сказать:
Я уже обрабатывала сообщение с таким внешним идентификатором. Повторно не принимаю.
3. Повторные попытки
Внешний мир падает.
Розетка должна уметь:
отправить сообщение; если не получилось — подождать; попробовать снова; если совсем плохо — положить в мертвую очередь.
4. Наблюдаемость
Каждая розетка должна быть видна в Dashboard:
сколько сообщений пришло; сколько ушло; сколько ошибок; какая задержка; какие сообщения застряли.
Это особенно важно, потому что внешние проблемы обычно сложнее диагностировать, чем внутренние.
________________________________________
9. Связь с вашими предыдущими принципами
Это очень красиво замыкает всю вашу архитектурную философию.
________________________________________
Принцип 1. Код как конституция
Раньше конституция описывала только внутренний мир:
кто из внутренних контекстов кого видит
Теперь она описывает и внешнюю границу:
какие внешние розетки разрешены; какие внешние сущности признаны; какие события могут пересекать границу.
Принцип 2. Буфер гравитации
Розеточная панель — это буфер против внешнего хаоса.
Разработчик больше не может сказать:
Я быстро подключусь к внешней Kafka напрямую из Trading Context
Потому что внешний мир доступен только через панель.
Принцип 3. Радар, Руль, Броня
Элемент	Как проявляется на внешнем периметре
Радар	телеметрия по каждой розетке
Руль	манифест внешних подключений в AppHost
Броня	идемпотентность, ретраи, мертвые очереди, антикоррупционный слой
________________________________________
10. Важное разделение: внутренний транспорт и внешний транспорт
Здесь нужно быть очень осторожным.
Внутри федерации вы справедливо решили начинать с простого:
HTTP + Outbox
Но внешний мир часто требует более тяжёлых инструментов:
Kafka для потоков данных; Rabbit для очередей задач; Redis для кэша и распределённых счётчиков; специализированные брокеры для торговых данных.
И это нормально.
Главное правило:
Внешняя сложность не должна протекать внутрь федерации.
Внутри контексты должны видеть только:
Integration Events Integration Commands Integration Queries
А розеточная панель сама решает, что снаружи это было:
Kafka message
Rabbit message
Redis pub/sub
HTTP callback
11. Взаимодействие с другими Aspire
Отдельный интересный случай — когда внешняя система тоже является Aspire-приложением.
Тогда возможны два варианта.
________________________________________
Вариант 1. Через HTTP
Чужой Aspire выставляет какой-то свой Gateway.
Ваша розеточная панель подключается к нему как к обычному внешнему HTTP-сервису.
Это самый простой и универсальный способ.
________________________________________
Вариант 2. Через общий брокер
Оба Aspire-приложения подключаются к одному внешнему брокеру, например к Kafka.
Тогда:
Ваш Aspire -> розеточная панель -> Kafka Чужой Aspire -> его интеграционный шлюз -> Kafka
И обе системы обмениваются событиями через нейтральную территорию.
Это хороший способ для интеграции независимых федераций.
________________________________________
12. Что я бы не делал
Я бы не делал несколько вещей.
________________________________________
1. Не позволял бы бизнес-контекстам напрямую подключаться к внешним брокерам
Плохо:
var tradingGateway = builder.AddProject<Projects.Trading_Gateway>("trading-gateway") .WithReference(externalKafka);
Правильно:
var integrationPanel = builder.AddProject<Projects.Integration_Panel>("integration-panel") .WithReference(externalKafka);
2. Не смешивал бы внутренние события с внешними
Не нужно использовать один и тот же контракт для:
внутреннего события; внешнего входящего сообщения; внешнего исходящего сообщения.
Лучше иметь три слоя:
Внешние сырые модели Адаптированные промежуточные модели Внутренние интеграционные контракты
3. Не строил бы розеточную панель как гигантский монолит
Пусть она будет единой точкой входа, но внутри должна быть разбита на адаптеры:
RabbitAdapter KafkaAdapter RedisAdapter HttpAdapter ExternalAspireAdapter
Иначе панель сама станет источником хаоса.
________________________________________
13. Итоговая формула
Если сжать всю мысль до нескольких строк:
Когда внутренние Bounded Context уже построены и управляются через свои Gateway, следующим слоем архитектуры должен стать внешний интеграционный периметр — отдельная сущность в Aspire, которая владеет всеми розетками для внешних систем.
Внутренний мир федерации остаётся чистым: он знает только свои интеграционные контракты.
Внешний мир существует только за розеточной панелью.
Каждая внешняя система подключается как отдельный адаптер, объявленный в манифесте, наблюдаемый в Dashboard и защищенный идемпотентностью, ретраями и антикоррупционным слоем.
14. Моя оценка вашей идеи
Я считаю, что вы снова поймали правильный архитектурный инстинкт.
Вы фактически описали завершение модели:
1. Сервис -> центральный Router 2. Контекст -> Context Gateway 3. Федерация -> Integration Panel
Это три уровня одной и той же идеи:
Любая сложность должна быть собрана в явной, управляемой, наблюдаемой границе.
И если внутренние границы вы уже научились строить, то внешняя розеточная панель — это естественный финальный контур вашей системы.
После этого архитектура становится по-настоящему законченной:
внутри — порядок; на границе — адаптация; снаружи — чужой хаос, который больше не может заразить систему.

Browser / Blazor Interactive Server
        |
        v
Trading.Web
        |
        | HTTP через логическое имя "gateway"
        v
Trading.Gateway
        |
        | YARP + Service Discovery
        |
        +------------------------+
        |                        |
        v                        v
Trading.Api.Orders       Trading.Api.MarketData