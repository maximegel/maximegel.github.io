using System;

namespace Samples.After
{
  public class IssueCommented : IDomainEvent<Issue>
  {
    public IssueCommented(IssueComment comment)
    {
      CommentId = comment.Id;
      Message = comment.Message;
    }

    public Guid CommentId { get; }
    public string Message { get; }

    // Events now know how to apply themselves.
    public void ApplyTo(Issue aggregate) =>
      aggregate.Comments.Add(new IssueComment(CommentId, Message));
  }
}
