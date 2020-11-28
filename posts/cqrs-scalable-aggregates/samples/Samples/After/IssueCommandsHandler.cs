using System;

namespace Samples.After
{
  public class IssueCommandsHandler : ICommandHandler<CommentIssue, Issue>
  {
    private readonly IRepository<Issue, Guid> _repository;

    public IssueCommandsHandler(IRepository<Issue, Guid> repository) => _repository = repository;

    public void Handle(CommentIssue command)
    {
      var issue = _repository.Find(command.IssueId);
      // The command can now be executed direcly on the aggregate.
      issue.Execute(command);
      _repository.Save(issue);
    }
  }
}
