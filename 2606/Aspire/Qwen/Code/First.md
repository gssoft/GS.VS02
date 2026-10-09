Обычный текст: пишется как есть.
Для заголовков используйте # (H1), ## (H2).
# Заголовок H1
## Заголовок H2

Встроенный код (однострочники): 
если вы упоминаете название класса или метода внутри предложения, берите его в одиночные обратные кавычки:

` csharp
public MyClass {
    int id;
    string str;
}
`
``` csharp
public MyClass {
    int id;
    string str;
}

var gateway = builder.AddProject<Projects.Trading_Gateway>("gateway")
    .WithReference(ordersApi)
    .WithReference(marketDataApi);

var web = builder.AddProject<Projects.Trading_Web>("web")
    .WithReference(gateway);

```

Этап 1:
- Оставить один Gateway
- Внутри маршруты: `/api/trading/*`, `/api/marketdata/*`

Этап 2:
- Выделить два Gateway: `Trading.Gateway`, `MarketData.Gateway`
``` csharp