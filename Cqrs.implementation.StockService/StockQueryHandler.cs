#nullable enable
using System.Collections.Generic;
using Cqrs.implementation.Infrastructure.Ports;
using Cqrs.implementation.Stock;
using Cqrs.implementation.Stock.Ports;

namespace Cqrs.implementation.Infrastructure
{
    // Read side: answers from the read model only. Never touches the event store or the write model.
    public class StockQueryHandler(IProductStockRepository readModel) :
        IQueryHandler<GetProductStock, ProductStockView?>,
        IQueryHandler<GetProductsInStock, IReadOnlyList<ProductStockView>>,
        IQueryHandler<CountProductsInStock, int>,
        IQueryHandler<GetStockAtDate, int>,
        IQueryHandler<GetStockVariation, int>
    {
        public ProductStockView? Handle(GetProductStock query) => readModel.GetProductStock(query.Ean);

        public IReadOnlyList<ProductStockView> Handle(GetProductsInStock query) => readModel.GetProductsInStock();

        public int Handle(CountProductsInStock query) => readModel.GetProductsInStock().Count;

        public int Handle(GetStockAtDate query) => readModel.GetStockAt(query.Ean, query.Date);

        public int Handle(GetStockVariation query) =>
            readModel.GetStockAt(query.Ean, query.To) - readModel.GetStockBefore(query.Ean, query.From);
    }
}
