using System;

namespace Samples.Before
{
  public class CommentIssue : ICommand
  {
    public CommentIssue(Guid issueId, string message)
    {
      IssueId = issueId;
      Message = message;
    }

    public Guid IssueId { get; }
    public string Message { get; }
  }
}
