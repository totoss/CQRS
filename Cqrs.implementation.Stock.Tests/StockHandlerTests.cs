#nullable enable
using System;
using System.Linq;
using Cqrs.implementation.Infrastructure;
using NUnit.Framework;

namespace Cqrs.implementation.Stock.Tests
{
    public class Tests
    {
        private EventRepository _events = null!;
        private StockCommandHandler _commands = null!;
        private StockQueryHandler _queries = null!;

        [SetUp]
        public void Setup()
        {
            _events = new EventRepository();
            _commands = new StockCommandHandler(_events);
            _queries = new StockQueryHandler(new ProductStockRepository(_events));
        }

        [Test]
        public void ShouldMakePurchaseOf20Units()
        {
            _commands.Handle(new PurchaseStock("EAN00001", 10, "Purchase N°1", DateTime.Now));
            _commands.Handle(new PurchaseStock("EAN00001", 10, "Purchase N°2", DateTime.Now));

            Assert.That(_queries.Handle(new GetProductsInStock()).Any());
            Assert.That(_queries.Handle(new GetProductStock("EAN00001"))!.Quantity, Is.EqualTo(20));
        }

        [Test]
        public void ShouldMakeOrderOf30Units()
        {
            _commands.Handle(new PurchaseStock("EAN00002", 10, "Purchase N°1", DateTime.Now));
            _commands.Handle(new OrderStock("EAN00002", 30, "Order N°1", DateTime.Now));

            Assert.That(_queries.Handle(new GetProductStock("EAN00002"))!.Quantity, Is.EqualTo(-20));
        }

        [Test]
        public void ShouldMakeInventoryMovement()
        {
            _commands.Handle(new TakeInventory("EAN00001", 10, DateTime.UtcNow));

            Assert.That(_queries.Handle(new GetProductStock("EAN00001"))!.Quantity, Is.EqualTo(10));
        }

        [Test]
        public void ShouldKnowStockAtGivenDate()
        {
            var stockDateNow = DateTime.UtcNow.Date;
            var stockDatePast = DateTime.UtcNow.Date.AddDays(-2);

            _commands.Handle(new PurchaseStock("EAN00001", 5, "Purchase N°1", stockDatePast));
            _commands.Handle(new PurchaseStock("EAN00001", 10, "Purchase N°2", stockDateNow));

            Assert.That(_queries.Handle(new GetStockAtDate("EAN00001", stockDatePast)), Is.EqualTo(5));
        }

        [Test]
        public void ShouldKnowStockOnADayWithoutMovement()
        {
            var stockDateNow = DateTime.UtcNow.Date;
            var stockDatePast = DateTime.UtcNow.Date.AddDays(-2);

            _commands.Handle(new PurchaseStock("EAN00001", 5, "Purchase N°1", stockDatePast));
            _commands.Handle(new PurchaseStock("EAN00001", 10, "Purchase N°2", stockDateNow));

            Assert.That(_queries.Handle(new GetStockAtDate("EAN00001", stockDatePast.AddDays(1))), Is.EqualTo(5));
            Assert.That(_queries.Handle(new GetStockAtDate("EAN00001", stockDateNow)), Is.EqualTo(15));
        }

        [Test]
        public void ShouldApplyChangesInDateOrderNotArrivalOrder()
        {
            var inventoryDate = DateTime.UtcNow;

            // Recorded first, but dated after the inventory: it counts on top of the inventory.
            _commands.Handle(new PurchaseStock("EAN00001", 10, "Purchase N°1", inventoryDate.AddHours(1)));
            _commands.Handle(new TakeInventory("EAN00001", 3, inventoryDate));

            Assert.That(_queries.Handle(new GetProductStock("EAN00001"))!.Quantity, Is.EqualTo(13));
        }

        [Test]
        public void ShouldKnowStockVariationsBetweenGivenDates()
        {
            var stockDateNow = DateTime.UtcNow.Date;
            var stockDatePast = DateTime.UtcNow.Date.AddDays(-2);

            _commands.Handle(new PurchaseStock("EAN00003", 5, "Purchase N°1", stockDatePast));
            _commands.Handle(new PurchaseStock("EAN00003", 10, "Purchase N°2", stockDateNow));

            Assert.That(_queries.Handle(new GetStockVariation("EAN00003", stockDatePast, stockDateNow)), Is.EqualTo(15));

            _commands.Handle(new OrderStock("EAN00003", 30, "Order N°1", stockDateNow));

            Assert.That(_queries.Handle(new GetStockVariation("EAN00003", stockDatePast, stockDateNow)), Is.EqualTo(-15));
        }

        [Test]
        public void ShouldGetProductsInStock()
        {
            var stockDateNow = DateTime.UtcNow.Date;
            var stockDatePast = DateTime.UtcNow.Date.AddDays(-2);

            _commands.Handle(new PurchaseStock("EAN00001", 5, "Purchase N°1", stockDatePast));
            _commands.Handle(new PurchaseStock("EAN00001", 10, "Purchase N°2", stockDateNow));

            Assert.That(_queries.Handle(new GetProductsInStock()).Any());

            _commands.Handle(new OrderStock("EAN00001", 100, "Order N°1", stockDateNow));

            Assert.That(!_queries.Handle(new GetProductsInStock()).Any());
        }

        [Test]
        public void ShouldExecuteSeveralStockMovement()
        {
            var stockDateNow = DateTime.UtcNow.Date;

            var purchaseCmd = new PurchaseStock("EAN00001", 5, "Purchase N°1", stockDateNow);
            _commands.Handle(purchaseCmd);

            var purchaseCmd2 = new PurchaseStock("EAN00002", 5, "Purchase N°2", stockDateNow);
            _commands.Handle(purchaseCmd2);

            var orderCommand = new PurchaseStock("EAN00001", 1, "Order N°1", stockDateNow);
            _commands.Handle(orderCommand);

            _commands.Handle(new RecordStockMovements([purchaseCmd, purchaseCmd2, orderCommand]));

            var inStock = _queries.Handle(new GetProductsInStock()).Select(x => x.Ean);
            Assert.That(inStock, Is.SupersetOf(new[] { "EAN00001", "EAN00002" }));
        }

        [Test]
        public void ShouldSaveNothingWhenOneMovementOfABatchIsRejected()
        {
            _commands.Handle(new TakeInventory("EAN00001", 0, DateTime.UtcNow));

            var batch = new RecordStockMovements([
                new PurchaseStock("EAN00002", 5, "Purchase N°1", DateTime.UtcNow),
                new PurchaseStock("EAN00001", 5, "Purchase N°2", DateTime.UtcNow.AddDays(-1))
            ]);

            Assert.Throws<InvalidOperationException>(() => _commands.Handle(batch));
            Assert.That(_queries.Handle(new GetProductStock("EAN00002")), Is.Null);
        }

        [Test]
        public void ShouldRejectSavingAStockChangedSinceItWasLoaded()
        {
            var first = new ProductStock("EAN00001", _events.GetEvents("EAN00001"));
            var stale = new ProductStock("EAN00001", _events.GetEvents("EAN00001"));

            first.Move(5, "Purchase N°1", DateTime.UtcNow);
            _events.Save([first]);

            stale.Move(5, "Purchase N°2", DateTime.UtcNow);
            Assert.Throws<InvalidOperationException>(() => _events.Save([stale]));
            Assert.That(_queries.Handle(new GetProductStock("EAN00001"))!.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void ShouldThrowInvalidOperationExceptionOnWrongInventoryDate()
        {
            var stockDate = DateTime.UtcNow.Date.AddDays(-20);

            _commands.Handle(new PurchaseStock("EAN00001", 5, "Purchase N°1", stockDate));
            _commands.Handle(new TakeInventory("EAN00001", 1, DateTime.UtcNow));

            var purchase2 = new PurchaseStock("EAN00001", 5, "Purchase N°2", stockDate);
            Assert.Throws<InvalidOperationException>(() => _commands.Handle(purchase2));
        }

        [Test]
        public void ShouldThrowArgumentExceptionExceptionIfInventoryMovementQuantityIsLowerThanZero()
        {
            Assert.Throws<ArgumentException>(() => _commands.Handle(new TakeInventory("EAN00001", -1, DateTime.UtcNow)));
        }
    }
}
