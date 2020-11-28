using System;

namespace Samples.Before
{
  public class CommentIssueHandler : ICommandHandler<CommentIssue>
  {
    private readonly IRepository<Issue, Guid> _repository;

    public CommentIssueHandler(IRepository<Issue, Guid> repository) => _repository = repository;

    public void Handle(CommentIssue command)
    {
      var issue = _repository.Find(command.IssueId);
      issue.Comment(command.Message);
      _repository.Save(issue);
    }
  }
}
