#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Cqrs.implementation.Infrastructure.Ports;
using Cqrs.implementation.Stock;

namespace Cqrs.implementation.Infrastructure
{
    // shortcut: in-memory, events are lost on restart; replace with a persistent event store when needed.
    public class EventRepository : IEventRepository
    {
        private readonly Dictionary<string, List<StockEvent>> _streams = new();
        private readonly object _lock = new();

        public event Action<StockEvent>? EventSaved;

        public IReadOnlyList<StockEvent> GetEvents(string ean)
        {
            lock (_lock)
                return _streams.TryGetValue(ean, out var stream) ? stream.ToList() : [];
        }

        public void Save(IReadOnlyCollection<ProductStock> stocks)
        {
            lock (_lock)
            {
                foreach (var stock in stocks)
                    if ((_streams.TryGetValue(stock.Ean, out var stream) ? stream.Count : 0) != stock.Version)
                        throw new InvalidOperationException($"Stock {stock.Ean} was changed by another command, retry.");

                var saved = stocks.SelectMany(s => s.Changes).ToList();
                foreach (var e in saved)
                {
                    if (!_streams.TryGetValue(e.Ean, out var stream))
                        _streams[e.Ean] = stream = new List<StockEvent>();
                    stream.Add(e);
                }

                // Notify only after everything is stored, so a failing subscriber can't leave half a batch saved.
                foreach (var e in saved)
                    EventSaved?.Invoke(e);
            }
        }
    }
}
