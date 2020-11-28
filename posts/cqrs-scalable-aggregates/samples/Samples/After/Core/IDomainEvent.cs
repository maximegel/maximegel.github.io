namespace Samples.After
{
  public interface IDomainEvent<TAggregate>
  {
    void ApplyTo(TAggregate aggregate);
  }
}
