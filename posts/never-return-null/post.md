---
title: Never Return Null Again
description: Understand how null affects code integrity and how it should be avoided.
coverImage: https://unsplash.com/photos/NCSARCecw4U
tags:
  - csharp
---

`null` is probably the worst mistake in programming languages history and has spread in all modern languages since its
invention in 1965. Many years later, [Tony Hoare](https://en.wikipedia.org/wiki/Tony_Hoare), the inventor of the
[null reference](https://en.wikipedia.org/wiki/Null_pointer), even apologies for his mistake:

> I call it my billion-dollar mistake. ... My goal was to ensure that all use of references should be absolutely safe,
> with checking performed automatically by the compiler. But I couldn't resist the temptation to put in a null
> reference, simply because it was so easy to implement. This has led to innumerable errors, vulnerabilities, and system
> crashes, which have probably caused a billion dollars of pain and damage in the last forty years.

In this article, I will explain how `null` affect code integrity and how we can avoid using it and most importantly
returning it to improve code reliability.

## What is wrong with `null`?

Well, let's give it a chance and see what happens. Imagine an e-commerce app allowing customers to get one discount each
month. Such a requirement could have been implemented like so:

```csharp
public IDiscountCoupon GetMonthlyDiscountCoupon()
{
  // some changes...
  if (MonthlyDiscountAlreadyUsed()) return null;
  return new RandomDiscountCoupon();
}
```

Notice that if the monthly discount has already been used, `null` is returned.

From the implementation point of view, this is pretty obvious the result can either be a `RandomDiscount` or `null`, but
what about the usage point of view?

```csharp
public void ApplyMonthlyDiscount(Order order)
{
  var coupon = GetMonthlyDiscountCoupon();
  // What now? How do I know if the next line will hit a null reference exception?
  // `coupon.ApplyDiscount(order);`
}
```

As you can see from the usage point of view, it is not that obvious. Of course, there is a chance you know `null` can be
returned, because you implemented both sides, you spent much time analyzing each used method or by some miracle, someone
decided to write documentation about it. Let's pretend it is the case for now so we can complete the above method:

```csharp
public void ApplyMonthlyDiscount(Order order)
{
  var coupon = GetMonthlyDiscountCoupon();
  if (coupon == null) return;
  coupon.ApplyDiscount(order);
}
```

Great, now we handle null reference exceptions, that was easy! Well, think about what could have happened if the "one
coupon by month limit" requirement was introduced after the first attempt of `ApplyMonthlyDiscount`. In that case,
`GetMonthlyDiscountCoupon` could have looked like this:

```csharp
public IDiscountCoupon GetMonthlyDiscountCoupon() => new RandomDiscountCoupon();
```

And so `ApplyMonthlyDiscount` could have never check for null references.

> Does that means we should always check for null references based on the fact that methods can change and return `null`
> in future?

**No**, as you can imagine this could quickly become a mess of null checks making the code lease readable and hard to
follow.

## Establishing the rule

A better solution is to establish an important programming rule across your team. This rule is simple:

> Never return `null`.

Of course, some verifications will have to be done to enforce this rule such as making it part of code reviews of
enabling some language features like the
[nullable reference types](https://devblogs.microsoft.com/dotnet/embracing-nullable-reference-types/) in C# 8.0 or the
[`strictNullChecks`](https://www.typescriptlang.org/docs/handbook/release-notes/typescript-2-0.html) flag in TypeScript.

Now that the rule is set, we can take it for granted and get rid of these annoying null checks!

Still, be aware that method arguments should still be validated. Values coming out can be controlled but values coming
in cannot. Take a look at [guard clauses][guard-clauses] for that.

Now what if we must return something that is really optional like in our discount coupon example, how do we do this
without breaking the rule?

## Special case pattern

One way to avoid returning `null` is to return a concrete implementation of the returned type that does nothing. This
technique is known as the [special case pattern](https://martinfowler.com/eaaCatalog/specialCase.html). Here is an
example from our previous scenario:

```csharp
// Default implementation:
public class RandomDiscountCoupon : IDiscountCoupon
{
  private readonly Random _random = new Random();

  public void ApplyDiscount(Order order) =>
    order.TotalPrice -= order.TotalPrice * _random.Next(5, 25) / 100;
}

// Special case implementation:
public class ZeroDiscountCoupon : IDiscountCoupon
{
  public void ApplyDiscount(Order order) {}
}
```

The important point here is that the special case class should be able to be used like any other implementations:

```csharp
public void ApplyMonthlyDiscount(Order order)
{
  var coupon = GetMonthlyDiscountCoupon();
  // No null checks and no special conditions specific to `ZeroDiscountCoupon`.
  coupon.ApplyDiscount(order);
}
```

This technique is very simple to understand and implement and should be used when possible, but in some scenarios, it
cannot be used. Let's tackle one of these with a more complex but more powerful approach.

## Optional object pattern

In some cases, the above solutions will not work and we will have to pull out more power and use the [optional object
pattern][optional-type]. Imagine we want our previous discount coupon to have a code and be attached to the order. In
the previous special case implementation it does not make any sense:

```csharp
// Interface:
public interface IDiscountCoupon
{
  // Added code property.
  string Code { get; }

  void ApplyDiscount(Order order);
}

// Special case implementation:
public class ZeroDiscountCoupon : IDiscountCoupon
{
  string Code { get; } = "";

  public void ApplyDiscount(Order order) {}
}
```

It does not look so bad, but it does not make much sense to attach a coupon with an empty code to the order:

```csharp
public void ApplyMonthlyDiscount(Order order)
{
  var coupon = GetMonthlyDiscountCoupon();
  coupon.ApplyDiscount(order);
  order.AttachCoupon(coupon);
  // Considering `order.AttachCoupon()` internally invokes `order.Coupon = coupon`,
  // `order.Coupon.Code` may now be an empty string.
}
```

A better solution is to return an optional object from `GetMonthlyDiscountCoupon` and to turn `Order.Coupon` into an
optional as well:

```csharp
// 1) Return an optional object from `GetMonthlyDiscountCoupon`.
public Optional<IDiscountCoupon> GetMonthlyDiscountCoupon()
{
  if (MonthlyDiscountAlreadyUsed()) return Optional.None<IDiscountCoupon>();
  return Optional.Some(new RandomDiscountCoupon());
}

public class Order
{
  // 2) Turn `Order.Coupon` into an optional.
  public Optional<IDiscountCoupon> Coupon { get; private set; } = Optional.None<IDiscountCoupon>();

  // Bonus tip: Use mutation methods and private setters to preserve encapsulation.
  public void AttachCoupon(IDiscountCoupon coupon) => Coupon = Optional.Some(coupon);
}
```

With this in place, the caller cannot access the coupon value directly and so is forced to make a decision:

```csharp
public void ApplyMonthlyDiscount(Order order)
{
  var optionalCoupon = GetMonthlyDiscountCoupon();
  // Evaluated if the coupon is present.
  optionalCoupon.MatchSome(coupon =>
  {
    coupon.ApplyDiscount(order);
    order.AttachCoupon(coupon);
  })
  // Otherwise, `order.Coupon` will remain none.
}
```

Unfortunately, unlike Java, C# lack the `Optional<T>` type. Implementing such a pattern is out of the scope of this
article but you take a look at [Zoran Horvat's great implementation][optional-type-impl].

But remember with great power comes great responsibility. This means implementing this pattern also means maintaining
additional code.

An alternative would be to use an open-source implementation such as
[Optional](https://www.nuget.org/packages/Optional/) in C#.

---

### Related

- [Guard Clauses Explained][guard-clauses]
- [Enabling C# Nullable Reference Types in the Whole Solution](https://stackoverflow.com/a/59522983/5960632)
- [How to Reduce Cyclomatic Complexity Part 5: Option Functional Type][optional-type] by Zoran Horvat
- [Custom Implementation of the Option/Maybe Type in C#][optional-type-impl] by Zoran Horvat

<!-- References: -->

[guard-clauses]: https://dev.to/maximegel/guard-clauses-explained-13aa
[optional-type]: http://codinghelmet.com/articles/reduce-cyclomatic-complexity-option-functional-type
[optional-type-impl]: http://codinghelmet.com/articles/custom-implementation-of-the-option-maybe-type-in-cs
