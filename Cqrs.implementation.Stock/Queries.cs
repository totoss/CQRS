using System;

namespace Cqrs.implementation.Stock
{
    // Queries are plain data: what the caller wants to read. They never change state.
    public sealed record GetProductStock(string Ean);

    public sealed record GetProductsInStock();

    public sealed record CountProductsInStock();

    public sealed record GetStockAtDate(string Ean, DateTime Date);

    // Change in stock over [From, To]: movements dated exactly at From are counted.
    public sealed record GetStockVariation(string Ean, DateTime From, DateTime To);

    // Read-only view returned by queries, never the write model.
    public sealed record ProductStockView(string Ean, int Quantity);
}
