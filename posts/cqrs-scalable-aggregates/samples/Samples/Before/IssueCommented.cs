using System;

namespace Samples.Before
{
  public class IssueCommented : IDomainEvent
  {
    public IssueCommented(IssueComment comment)
    {
      CommentId = comment.Id;
      Message = comment.Message;
    }

    public Guid CommentId { get; }
    public string Message { get; }
  }
}
