using FluentAssertions;
using Samples.Before;
using System.Linq;
using Xunit;

namespace Samples.Tests.Before
{
  public class CommentIssueTests
  {
    [Fact]
    public void Comment_WithEmptyMessage_DoesNothing()
    {
      var issue = new Issue();

      issue.Comment(" ");
      var events = issue.Commit();

      events.Should().BeEmpty();
      issue.Comments.Should().BeEmpty();
    }

    [Fact]
    public void Comment_WithNonEmptyMessage_Emits()
    {
      var issue = new Issue();

      issue.Comment("Any updates on this?");
      var events = issue.Commit();

      events.Should().HaveCount(1).And.AllBeOfType<IssueCommented>();
      issue.Comments.Select(c => c.Message).Should().Contain("Any updates on this?");
    }
  }
}
