namespace Samples.After
{
  public interface IRepository<TAggregate, TId>
    where TAggregate : IAggregateRoot<TId>
  {
    TAggregate Find(TId id);

    void Save(TAggregate aggregate);
  }
}
