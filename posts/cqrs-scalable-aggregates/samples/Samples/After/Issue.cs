using System;
using System.Collections.Generic;

namespace Samples.After
{
  public class Issue : AggregateRoot<Issue, Guid>
  {
    public Issue() : this(Guid.NewGuid()) { }

    public Issue(Guid id) : base(id) { }

    public ISet<IssueComment> Comments { get; } = new HashSet<IssueComment>();

    // Only common validations/business rules go here!
  }
}
