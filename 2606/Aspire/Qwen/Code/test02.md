# Примеры

Это обычный текст.

Он пишется как есть.

Для заголовков используется символ решетки.

## Заголовок H1

## Заголовок H2

### Заголовок H3

Главная идея: я бы строил взаимодействие между Bounded Context-ами по такому принципу:
Внутри контекста сервисы могут быть любыми.
Снаружи контекст имеет только одну публичную границу — свой Gateway.
Взаимодействие между контекстами происходит только через их Gateway или через явный интеграционный механизм, объявленный в AppHost.

### Встроенный код

Для упоминания типа используйте `MyClass`.

### Блок кода (C#)

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

```csharp
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

**текст** qwerty

> Dorothy followed her through many of the beautiful rooms in her castle.
