using System.Collections.Generic;

namespace Samples.After
{
  public interface ICommand<TAggregate>
    where TAggregate : IAggregateRoot
  {
    IEnumerable<IDomainEvent<TAggregate>> ExecuteOn(TAggregate aggregate);
  }
}
