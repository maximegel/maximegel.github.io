using System.Collections.Generic;

namespace Samples.Before
{
  public abstract class AggregateRoot<TSelf, TId> : Entity<TSelf, TId>, IAggregateRoot<TId>
    where TSelf : AggregateRoot<TSelf, TId>
  {
    private readonly List<IDomainEvent> _uncommitedEvents = new List<IDomainEvent>();

    protected AggregateRoot(TId id) : base(id) { }

    // Invoked by the repository to collect and store events.
    public IEnumerable<IDomainEvent> Commit()
    {
      var events = new List<IDomainEvent>(_uncommitedEvents);
      _uncommitedEvents.Clear();
      return events;
    }

    protected abstract void Apply(IDomainEvent @event);

    protected void Emit(IDomainEvent @event)
    {
      _uncommitedEvents.Add(@event);
      Apply(@event);
    }
  }
}
