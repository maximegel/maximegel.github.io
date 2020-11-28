using FluentAssertions;
using Samples.After;
using System.Linq;
using Xunit;

namespace Samples.Tests.After
{
  public class CommentIssueTests
  {
    [Fact]
    public void Comment_WithEmptyMessage_DoesNothing()
    {
      var issue = new Issue();
      var command = new CommentIssue(issue.Id, " ");

      issue.Execute(command);
      var events = issue.Commit();

      events.Should().BeEmpty();
      issue.Comments.Should().BeEmpty();
    }

    [Fact]
    public void Comment_WithNonEmptyMessage_Emits()
    {
      var issue = new Issue();
      var command = new CommentIssue(issue.Id, "Any updates on this?");

      issue.Execute(command);
      var events = issue.Commit();

      events.Should().HaveCount(1).And.AllBeOfType<IssueCommented>();
      issue.Comments.Select(c => c.Message).Should().Contain("Any updates on this?");
    }
  }
}
