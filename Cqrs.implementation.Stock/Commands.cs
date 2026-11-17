using System;
using System.Collections.Generic;

namespace Cqrs.implementation.Stock
{
    // Commands are plain data: what the caller wants to change. Handlers decide and record it.
    public abstract record StockMovementCommand(string Ean, int Quantity, string Label, DateTime Date);

    public sealed record PurchaseStock(string Ean, int Quantity, string Label, DateTime Date)
        : StockMovementCommand(Ean, Quantity, Label, Date);

    public sealed record OrderStock(string Ean, int Quantity, string Label, DateTime Date)
        : StockMovementCommand(Ean, Quantity, Label, Date);

    // All movements are saved, or none.
    public sealed record RecordStockMovements(IReadOnlyList<StockMovementCommand> Movements);

    public sealed record TakeInventory(string Ean, int Quantity, DateTime Date);
}
