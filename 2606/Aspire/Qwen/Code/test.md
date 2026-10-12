
# Примеры

Это обычный текст.

Он пишется как есть

Для заголовков используется символ решетки


## Заголовок H2

### Заголовок H3

#### Заголовок H4

Главная идея
Я бы строил взаимодействие между Bounded Context-ами по такому принципу:
Внутри контекста сервисы могут быть любыми.
Снаружи контекст имеет только одну публичную границу — свой Gateway.
Взаимодействие между контекстами происходит только через их Gateway или через явный интеграционный механизм, объявленный в AppHost.

`csharp
public MyClass {
    int id;
    string str;
}
`

```csharp
public MyClass {
    int id;
    string str;
}
```

```csharp
var gateway = builder.AddProject<Projects.Trading_Gateway>("gateway")
    .WithReference(ordersApi)
    .WithReference(marketDataApi);

var web = builder.AddProject<Projects.Trading_Web>("web")
    .WithReference(gateway);
```
