---
title: CQRS Scalable Aggregates
description: A way to reduce complexity from your aggregates and make them scale better
coverImage: https://unsplash.com/photos/vGQ49l9I4EE
tags:
  - csharp
  - domain-driven-design
  - cqrs
  - event-sourcing
---

Aggregates are the heart of your system. They hold domain logic and are responsible for emitting events that will
eventually make your data consistent across multiples data projections and bounded contexts. Because aggregates are so
important, it is crucial to keep them small and easy to understand, but how to do this when the domain gets bigger and
bigger?

In this article, I will show you a way to reduce complexity from your aggregates and make them scale better.

## Write-side domain overview

Before getting started, let review how commands are executed and turned into events.

After receiving the command sent by the user, the command handler invokes the repository to load the aggregate state in
memory. Then, the command is executed on the aggregate which emits either domain events or failures. Finally, those
events or failures are persisted using the repository.

e.g.

```text
[👤 Coder]
  -> [📨 Comment issue command]
  -> [📫 Comment issue handler]
  -> [🍇 Issue aggregate]
  -> [📅 Issue commented event]
```

## First attempt

In the first place, I will show you briefly how the write-side domain was implemented in a real-world project a worked
on. Then, we will discuss the scaling issues of this solution and how it can be improved to keep your aggregates clean.

Here is how it looks:

```csharp
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

public class CommentIssueHandler : ICommandHandler<CommentIssue>
{
  private readonly IRepository<Issue> _repository;

  public CommentIssueHandler(IRepository<Issue> repository) => _repository = repository;

  public void Handle(CommentIssue command)
  {
    // 1. Loads the aggregate in memory.
    var issue = _repository.Find(command.IssueId);
    // 2. Invokes the aggregate method.
    issue.Comment(command.message);
    // 3. Saves the aggregate.
    _repository.Save(issue);
  }
}

public class Issue : AggregateRoot<Issue, Guid>
{
  private readonly ISet<IssueComment> _comments = new HashSet<IssueComment>();

  public Issue(Guid id) : base(id) { }

  public IEnumerable<IssueComment> Comments => _comments;

  public void Comment(string message)
  {
    if (string.IsNullOrWhiteSpace(message)) return;
    // `Emit()` internally invokes `Apply()` to avoid code duplication.
    Emit(new IssueCommented(new IssueComment(message)));
  }

  // All other command related methods go here e.g.
  // `public void Edit() { }`
  // `public void Close() { }`
  // `public void Unsubscribe() { }`

  protected override void Apply(IDomainEvent @event)
  {
    // This could be done using reflexion, but be aware of performance issues.
    switch (@event)
    {
      case IssueCommented e: Apply(e); break;
    }
  }

  private void Apply(IssueCommented @event) =>
    _comments.Add(new IssueComment(@event.CommentId, @event.Message));
}

public class IssueCommented : IDomainEvent
{
  public IssueCommented(Comment comment)
  {
    CommentId = comment.Id;
    Message = comment.Message;
  }

  public Guid CommentId { get; }
  public string Message { get; }
}
```

As you can see, using this technique, the aggregate grows for every command we add. Because of this, it is easy to
imagine how fast this newly created aggregate will turn into a hard to maintain mess as our domain grows.

Also, all command handlers will do the exact same things i.e.:

1. Loads the aggregate in memory.
2. Invokes the aggregate method.
3. Saves the aggregate.

Let see how we can fix this!

## Refactored solution

The trick is to let the commands and events execute/apply themselves!

```csharp
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
```

By doing this, we are able to implement a new `Execute()` method in the aggregate base class which will execute a
command and apply the returned events.

Also, it makes it possible to generalize the `Apply()` method and move it up to the aggregate base class.

```csharp
public abstract class AggregateRoot<TSelf, TId> : Entity<TSelf, TId>, IAggregateRoot<TId>
  where TSelf : AggregateRoot<TSelf, TId>
{
  private readonly List<IDomainEvent<TSelf>> _uncommittedEvents = new List<IDomainEvent<TSelf>>();

  // ...

  public void Apply(IDomainEvent<TSelf> @event) => @event.ApplyTo((TSelf)this);

  public void Execute(ICommand<TSelf> command)
  {
    var events = command.ExecuteOn((TSelf)this);
    _uncommittedEvents.AddRange(events);
    foreach (var @event in events) Apply(@event);
  }
}
```

Now that we added the `Execute()` method we can invoke it right away from the command handler.

Also, you can see below, our command handler is now named `IssueCommandsHandler` instead of `CommentIssueHandler`. Why
is that? This is because we simplified enough that all command handlers will be exactly the same so we can use the same
command handle for all commands.

```csharp
public class IssueCommandsHandler : ICommandHandler<CommentIssue, Issue>
{
  private readonly IRepository<Issue, Guid> _repository;

  public IssueCommandsHandler(IRepository<Issue, Guid> repository) => _repository = repository;

  public void Handle(CommentIssue command)
  {
    var issue = _repository.Find(command.IssueId);
    // The command can now be executed directly on the aggregate.
    issue.Execute(command);
    _repository.Save(issue);
  }
}
```

Our objective was to clean up our aggregate and as you can see below there is nothing left in it so I think we made it!

```csharp
public class Issue : AggregateRoot<Issue, Guid>
{
  public Issue(Guid id) : base(id) { }

  public ISet<IssueComment> Comments { get; } = new HashSet<IssueComment>();

  // Only common validations/business rules go here!
}
```

Note that I exposed `Comments.Add()` through the `ISet` interface for simplicity purposes. In a real-world project,
consider adding a custom collection class such as `IssueComments` to hold custom business rules.

---

The source code can be found
[here](https://github.com/maximegel/maximegel.github.io/tree/master/blog/cqrs-scalable-aggregates/samples).

### Related

- [Implementing an Event Sourced Aggregate](https://buildplease.com/pages/fpc-9/) by Nick Chamberlain
- [Command Handlers](https://buildplease.com/pages/fpc-10/) by Nick Chamberlain
