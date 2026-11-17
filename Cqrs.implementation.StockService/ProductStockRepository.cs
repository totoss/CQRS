#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Cqrs.implementation.Infrastructure.Ports;
using Cqrs.implementation.Stock;

namespace Cqrs.implementation.Infrastructure
{
    // Read model: its own copy of each product's stock changes, kept sorted by date for date queries.
    public class ProductStockRepository : IProductStockRepository
    {
        private readonly Dictionary<string, List<StockEvent>> _changes = new();
        private readonly object _lock = new();

        // shortcut: only sees events saved after it is created; replay the event store at startup once it persists.
        public ProductStockRepository(IEventRepository events)
        {
            events.EventSaved += Project;
        }

        private void Project(StockEvent e)
        {
            lock (_lock)
            {
                if (!_changes.TryGetValue(e.Ean, out var changes))
                    _changes[e.Ean] = changes = new List<StockEvent>();
                // After every change with the same date, so same-date changes keep their save order.
                changes.Insert(changes.FindLastIndex(x => x.Date <= e.Date) + 1, e);
            }
        }

        public ProductStockView? GetProductStock(string ean)
        {
            lock (_lock)
                return _changes.TryGetValue(ean, out var changes) ? new ProductStockView(ean, Level(changes, _ => true)) : null;
        }

        public IReadOnlyList<ProductStockView> GetProductsInStock()
        {
            lock (_lock)
                return _changes
                    .Select(x => new ProductStockView(x.Key, Level(x.Value, _ => true)))
                    .Where(x => x.Quantity > 0)
                    .ToList();
        }

        public int GetStockAt(string ean, DateTime date) => Level(ean, x => x.Date <= date);

        public int GetStockBefore(string ean, DateTime date) => Level(ean, x => x.Date < date);

        private int Level(string ean, Func<StockEvent, bool> counted)
        {
            lock (_lock)
                return _changes.TryGetValue(ean, out var changes) ? Level(changes, counted) : 0;
        }

        // An inventory sets the level, a movement adds to it.
        // shortcut: replays a product's changes on every read; store running totals if histories get long.
        private static int Level(List<StockEvent> changes, Func<StockEvent, bool> counted) =>
            changes.TakeWhile(counted).Aggregate(0, (level, e) => e switch
            {
                InventoryTaken inventory => inventory.Quantity,
                StockMoved moved => level + moved.Quantity,
                _ => throw new NotSupportedException(e.GetType().Name)
            });
    }
}
