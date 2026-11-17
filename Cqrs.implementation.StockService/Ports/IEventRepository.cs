#nullable enable
using System;
using System.Collections.Generic;
using Cqrs.implementation.Stock;

namespace Cqrs.implementation.Infrastructure.Ports
{
    // Write-side store: the stock event log.
    public interface IEventRepository
    {
        // Raised for each saved event, in save order, once the whole save is stored.
        event Action<StockEvent>? EventSaved;

        IReadOnlyList<StockEvent> GetEvents(string ean);

        // All or nothing: throws if any of these stocks was changed since it was loaded.
        void Save(IReadOnlyCollection<ProductStock> stocks);
    }
}
