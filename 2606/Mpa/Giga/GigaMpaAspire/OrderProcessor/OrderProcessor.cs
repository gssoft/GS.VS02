// Этот файл НЕ будет перезаписан генератором благодаря модификатору partial
namespace Generated
{
    // Генератор создаст абстрактный базовый класс OrderProcessorBase,
    // а этот partial-класс наследуется от него.
    public partial class OrderProcessor
    {
        protected override async Task OnOrderPaid(OrderPaid @event, CancellationToken ct)
        {
            // ВАША БИЗНЕС-ЛОГИКА
            Console.WriteLine($"Обрабатываем оплату заказа {@event.OrderId} на сумму {@event.Amount}");

            // Методы-помощники уже сгенерированы в Base-классе!
            await PublishInventoryReserved(new InventoryReserved(@event.OrderId, "SKU-123"), ct);
            await PublishInvoiceCreated(new InvoiceCreated(@event.OrderId, @event.Amount), ct);
        }
    }
}

