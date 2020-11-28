using System.Collections.Generic;

namespace Samples.After
{
  public abstract class AggregateRoot<TSelf, TId> : Entity<TSelf, TId>, IAggregateRoot<TId>
    where TSelf : AggregateRoot<TSelf, TId>
  {
    private readonly List<IDomainEvent<TSelf>> _uncommitedEvents = new List<IDomainEvent<TSelf>>();

    protected AggregateRoot(TId id) : base(id) { }

    // Invoked by the repository to collect and store events.
    public IEnumerable<IDomainEvent<TSelf>> Commit()
    {
      var events = new List<IDomainEvent<TSelf>>(_uncommitedEvents);
      _uncommitedEvents.Clear();
      return events;
    }

    public void Apply(IDomainEvent<TSelf> @event) => @event.ApplyTo((TSelf)this);

    public void Execute(ICommand<TSelf> command)
    {
      var events = command.ExecuteOn((TSelf)this);
      _uncommitedEvents.AddRange(events);
      foreach (var @event in events) Apply(@event);
    }
  }
}
