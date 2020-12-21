---
title: Guard Clauses Explained
description: A way to protect your methods against invalid inputs and outputs.
coverImage: https://unsplash.com/photos/9HnSQn4TVEg
tags:
  - csharp
---

## What is a guard clause?

A guard clause is a technique derived from the [fail-fast](https://en.wikipedia.org/wiki/fail-fast) method whose purpose
is to validate a condition and immediately stop the code execution if the condition is not met by throwing a meaningful
error instead of leaving the program to throw a deeper and less meaningful error. Let me show you an example:

```csharp
public User GetUser(string userName)
{
  if (userName == null) throw new NullArgumentException(nameof(userName));
  // Continue if the condition is fulfilled...
}
```

In the previous example, the `if` block act as a guard clause by protecting the `GetUser` method against any null
`userName` arguments. Like that, we are free to write the rest of the method without having to worry about checking if
`userName` is null.

## Why using guard clauses?

Guard clauses simplify code by removing useless nested branching conditions, by returning meaningful errors.

**Before guard clauses:**

```csharp
public void CreateUser(string userName, string password)
{
  if (userName == null)
  {
    // Do something...
  }
  else
  {
    // Do something else...
    if (password == null)
    {
      // Do something...
    }
    else
    {
      // Do something else...
    }
  }
}
```

**After guard clauses:**

```csharp
public void CreateUser(string userName, string password)
{
  if (userName == null)
    throw new NullArgumentException(nameof(userName));
  if (password == null)
    throw new NullArgumentException(nameof(password));
  // Do something...
}
```

## What to guard against?

### Pre-conditions

Pre-conditions are the conditions that have to be met before the method execution. Basically, the method pre-conditions
will always **depend on the method arguments**. A good example of a method pre-condition is a non-null argument.

```csharp
public User GetUser(string userName)
{
  // Requires a non null `userName` argument.
  if (userName == null) throw new NullArgumentException(nameof(userName));
  // Execute the method purpose...
}
```

### Post-conditions

Pre-conditions are the conditions that have to be met after the method execution. Basically, the method post-conditions
will always **depend on the value returned by the method**. A good example of a method post-condition is a non-null or a
not empty string.

```csharp
public User GetUsers()
{
  // Execute the method purpose:
  var users = this._repository.GetUsers();
  // Ensures `UserRepository.GetUsers()` returns no null users.
  if (users.Any(user => user == null))
    throw new Exception("UserRepository.GetResult() a null user.");
  return users;
}
```

As you can guess, post-conditions are not mandatory since you know exactly what's coming out of the method. The only
time you should consider using post-conditions is when you have to use unreliable calls (i.e. external methods or
protected methods) to get the result of the method as the example above.

### Public vs private

In both cases, pre-conditions and post-conditions should not be checked for private methods since the class itself is
the caller of these methods, you can trust what goes in goes and out of them. So you don't have to validate them.

## How to handle guard clauses exceptions?

Ok, it's time to introduce a very important rule about guard clause:

> Guard clauses exceptions should never be caught.

What this means is that most of the time, you should let the caller hit those exceptions because most of the time, guard
clauses will guard against scenarios that should never happen like null arguments. What a null argument means? Most of
the time, a null argument is a bug so should we catch a bug and taking the chance of never discover it? No! Instead, we
want to let the application fail immediately so that we can discover the bug before deploying it to production during
the development process.

But what if we have pre-conditions that don't rely on bugs? What if we have pre-conditions that could occur sometimes
like business logic pre-conditions? The solution is to expose your guard clauses!

## Why exposing guard clauses?

Sometimes, you have to guard against business logic which means the condition may not be respected and it's not
necessarily a bug. In those cases, a possibility is to expose the related guard clauses under a public boolean method to
let the caller branch around this boolean method.

**Implementation:**

```csharp
public bool CanCreateUser(string userName, string password)
{
  return !this.UserNameExists(userName) && this.IsStrongPassword(password);
}

public void CreateUser(string userName, string password)
{
  if (!this.CanCreateUser(userName, password))
    throw new Exception("The user can't be created.");
  // Create the user...
}
```

**Usage:**

```csharp
if (!CanCreateUser(userName, password))
{
  Console.WriteLine("Invalid username or password.");
  return;
}
CreateUser(userName, password);
Console.WriteLine("The user has been successfully created.");
```

## Why creating a guard class

It's a good practice to encapsulate our guard clauses inside a class dedicated to providing guard clauses so we could
reuse the logic and write more readable guard clauses. Here is an example of such a class:

```csharp
public static Guard
{
  public static void Requires(Func<bool> predicate, string message)
  {
    if (predicate()) return;
    Debug.Fail(message);
    throw new GuardClauseException(message);
  }

  [Conditional("DEBUG")]
  public static void Ensures(Func<bool> predicate, string message)
  {
    Debug.Assert(predicate(), message);
  }
}
```

In that implementation, the `Requires` method is used to validate pre-conditions and the `Ensures` method is to validate
post-conditions. The interesting point about this implementation is the use of the C# `Debug` class coming from the
`System.Diagnostics` namespace. The main point of the `Debug` class is that it will execute in debug mode only and can't
be caught by the caller so it's respect the "never catch guard clauses exceptions" rule. Also, the `Ensures` method uses
the `Conditional` C# attribute to make sure the code inside the method will run in debugging only so the performance
will not be affected in production because remember the guard clauses bugs will be detected before deploying in
production! But just to be sure, we protect ourselves in the Requires method by throwing a `GuardClauseException` if the
precondition isn't met because the code after it will probably fail anyway or cause data inconsistency we don't want.

After all, here how we could use the `Guard` class:

```csharp
public User GetUserRange(int from, int to)
{
  Guard.Requires(() => from >= 0, "Received negative from.");
  Guard.Requires(() => to >= 0, "Received negative to.");

  var users = this._repository.GetUsers();
  foreach (user in result)
    Guard.Ensures(
      () => user != null,
      "UserRepository.GetResult() a null user.");

  return users;
}
```

## Summary

In this article, we have seen how guard clauses can help us to discover bugs before deploying in production and to make
our code more readable.

We have also learned, why guard clauses exceptions should never be handled and how to expose guard clauses as a
draw-back when they rely on business logic.

Finally, we learned how to create our own guard clauses implementation under the `Guard` class.
