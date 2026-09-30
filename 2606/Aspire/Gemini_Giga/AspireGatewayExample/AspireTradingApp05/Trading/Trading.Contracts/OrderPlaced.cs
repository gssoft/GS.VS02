namespace Trading.Contracts;

/// <summary>
/// Событие, публикуемое после успешного размещения ордера в Trading Context.
/// </summary>
/// <param name="OrderId">Уникальный идентификатор ордера.</param>
/// <param name="Symbol">Торговый символ (например, "AAPL").</param>
/// <param name="Price">Цена, по которой был размещен ордер.</param>
/// <param name="Quantity">Количество.</param>
/// <param name="Side">Сторона сделки: "Buy" или "Sell".</param>
/// <param name="OccurredAt">Время размещения ордера в UTC.</param>
public record OrderPlaced(
    Guid OrderId,
    string Symbol,
    decimal Price,
    int Quantity,
    string Side,
    DateTime OccurredAt);

