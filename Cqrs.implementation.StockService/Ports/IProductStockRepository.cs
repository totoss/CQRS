#nullable enable
using System;
using System.Collections.Generic;
using Cqrs.implementation.Stock;

namespace Cqrs.implementation.Infrastructure.Ports
{
    // Read-side store, fed by the write side's events.
    public interface IProductStockRepository
    {
        ProductStockView? GetProductStock(string ean);

        IReadOnlyList<ProductStockView> GetProductsInStock();

        // Stock once every movement dated at or before `date` is applied.
        int GetStockAt(string ean, DateTime date);

        // Stock once every movement dated strictly before `date` is applied.
        int GetStockBefore(string ean, DateTime date);
    }
}
