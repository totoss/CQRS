#nullable enable
using System;
using System.Collections.Generic;

namespace Cqrs.implementation.Stock
{
    // Write model for one product: rebuilt from its events, enforces the stock rules, records new events.
    // It only keeps the state its rules need; quantities are the read model's job.
    public sealed class ProductStock
    {
        private readonly List<StockEvent> _changes = new();
        private DateTime? _lastInventoryDate;

        public string Ean { get; }

        // Number of stored events this was built from, checked on save to catch concurrent writers.
        public int Version { get; }

        public IReadOnlyList<StockEvent> Changes => _changes;

        public ProductStock(string ean, IReadOnlyList<StockEvent> history)
        {
            if (ean.Length != 8)
                throw new ArgumentException("Ean must have length of 8 characters!");

            Ean = ean;
            foreach (var e in history)
                Apply(e);
            Version = history.Count;
        }

        public void Move(int quantity, string label, DateTime date)
        {
            if (_lastInventoryDate is { } lastInventory && date <= lastInventory)
                throw new InvalidOperationException("It is not possible to add a movement at a date earlier than or equal to that of an inventory.");

            Record(new StockMoved(Ean, quantity, label, date));
        }

        public void TakeInventory(int quantity, DateTime date)
        {
            if (quantity < 0)
                throw new ArgumentException("Inventory movements are unique and cannot be negative");

            Record(new InventoryTaken(Ean, quantity, date));
        }

        private void Record(StockEvent e)
        {
            Apply(e);
            _changes.Add(e);
        }

        private void Apply(StockEvent e)
        {
            if (e is InventoryTaken inventory)
                _lastInventoryDate = inventory.Date;
        }
    }
}
