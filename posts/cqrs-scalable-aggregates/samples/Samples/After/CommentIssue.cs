using System;
using System.Collections.Generic;

namespace Samples.After
{
  public class CommentIssue : ICommand<Issue>
  {
    public CommentIssue(Guid issueId, string message)
    {
      IssueId = issueId;
      Message = message;
    }

    public Guid IssueId { get; }
    public string Message { get; }

    // Commands now know how to execute themselves.
    public IEnumerable<IDomainEvent<Issue>> ExecuteOn(Issue aggregate)
    {
      if (string.IsNullOrWhiteSpace(Message)) yield break;
      yield return new IssueCommented(new IssueComment(IssueId, Message));
    }
  }
}
