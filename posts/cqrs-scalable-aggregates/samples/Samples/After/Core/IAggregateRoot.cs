namespace Samples.After
{
  public interface IAggregateRoot : IEntity
  {
  }

  public interface IAggregateRoot<TId> : IAggregateRoot, IEntity<TId>
  {
  }
}
