namespace MicroPlatform.Core.Events;

public sealed record OrderPaid(Guid OrderId, decimal Amount);

public sealed record InventoryReserved(Guid OrderId, string Sku);
