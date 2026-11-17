using System;

namespace Cqrs.implementation.Stock
{
    // Facts recorded by the write side. The event log is the source of truth; read models are built from it.
    public abstract record StockEvent(string Ean, DateTime Date);

    // Quantity is signed: positive for a purchase, negative for an order.
    public sealed record StockMoved(string Ean, int Quantity, string Label, DateTime Date) : StockEvent(Ean, Date);

    public sealed record InventoryTaken(string Ean, int Quantity, DateTime Date) : StockEvent(Ean, Date);
}
