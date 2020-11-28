using System;

namespace Samples.Before
{
  public class IssueComment : Entity<IssueComment, Guid>
  {
    public IssueComment(string message) : this(Guid.NewGuid(), message) { }

    public IssueComment(Guid id, string message) : base(id) => Message = message;

    public string Message { get; }
  }
}
