using System;
using System.Collections.Generic;

namespace Samples.Before
{
  public class Issue : AggregateRoot<Issue, Guid>
  {
    private readonly ISet<IssueComment> _comments = new HashSet<IssueComment>();

    public Issue() : this(Guid.NewGuid()) { }

    public Issue(Guid id) : base(id) { }

    public IEnumerable<IssueComment> Comments => _comments;

    public void Comment(string message)
    {
      if (string.IsNullOrWhiteSpace(message)) return;
      // `Emit()` internally invokes `Apply()` to avoid code duplication.
      Emit(new IssueCommented(new IssueComment(message)));
    }

    // All other command related methods go here e.g.:
    // `public void Edit() { }`
    // `public void Close() { }`
    // `public void Unsubscribe() { }`

    protected override void Apply(IDomainEvent @event)
    {
      // This could be done using reflexion, but be aware of performance issues.
      switch (@event)
      {
        case IssueCommented e: Apply(e); break;
      }
    }

    private void Apply(IssueCommented @event) =>
      _comments.Add(new IssueComment(@event.CommentId, @event.Message));
  }
}
