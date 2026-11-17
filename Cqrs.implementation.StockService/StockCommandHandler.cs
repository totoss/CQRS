#nullable enable
using System;
using System.Collections.Generic;
using Cqrs.implementation.Infrastructure.Ports;
using Cqrs.implementation.Stock;
using Cqrs.implementation.Stock.Ports;

namespace Cqrs.implementation.Infrastructure
{
    // Write side: loads the stock, lets it check the rules, saves the new events. Never reads the read model.
    public class StockCommandHandler(IEventRepository events) :
        ICommandHandler<PurchaseStock>,
        ICommandHandler<OrderStock>,
        ICommandHandler<RecordStockMovements>,
        ICommandHandler<TakeInventory>
    {
        public void Handle(PurchaseStock command) => Handle(new RecordStockMovements([command]));

        public void Handle(OrderStock command) => Handle(new RecordStockMovements([command]));

        public void Handle(RecordStockMovements command)
        {
            // One stock per product, every movement checked before anything is saved.
            var stocks = new Dictionary<string, ProductStock>();
            foreach (var movement in command.Movements)
            {
                if (!stocks.TryGetValue(movement.Ean, out var stock))
                    stocks[movement.Ean] = stock = Load(movement.Ean);

                var quantity = movement switch
                {
                    PurchaseStock => movement.Quantity,
                    OrderStock => -movement.Quantity,
                    _ => throw new NotSupportedException(movement.GetType().Name)
                };
                stock.Move(quantity, movement.Label, movement.Date);
            }

            events.Save(stocks.Values);
        }

        public void Handle(TakeInventory command)
        {
            var stock = Load(command.Ean);
            stock.TakeInventory(command.Quantity, command.Date);
            events.Save([stock]);
        }

        private ProductStock Load(string ean) => new(ean, events.GetEvents(ean));
    }
}
