namespace Cqrs.implementation.Stock.Ports
{
    public interface ICommandHandler<in TCommand>
    {
        void Handle(TCommand command);
    }

    public interface IQueryHandler<in TQuery, out TResult>
    {
        TResult Handle(TQuery query);
    }
}
