namespace Samples.After
{
  public interface ICommandHandler<TCommand, TAggregate>
    where TCommand : ICommand<TAggregate>
    where TAggregate : IAggregateRoot
  {
    void Handle(TCommand command);
  }
}
