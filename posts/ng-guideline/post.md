---
title: Angular Opinionated Guide
description:
coverImage: ./assets/cover.png
tags:
  - angular
  - front-end-development
  - web-development
---

This guide extend without overriding the [official Angular guide](https://angular.io/guide/styleguide) as well as
following the same [style vocabulary](https://angular.io/guide/styleguide#style-vocabulary).

Take a look at this [example repository](https://github.com/maximegel/ngrx-tour-of-heroes) to see the following
guidelines in action.

Without further introduction, let's dive in!

---

## Components

### Encapsulate dependencies

**Do** create a `@NgModule()` for each component.

**Do** place the component module in the same named folder as the component.

```text
|- hero-list
|  |- hero-list.component.{ts,html,css,spec.ts}
|  |- hero-list.module.ts
```

**Why?** By encapsulating their dependencies in a module, each component can be used and tested independently.

**Why?** Component dependencies can be replaced and removed without affecting other components.

**Why?** Components can easily be moved around with their dependencies.

### Change detection

**Do** use the `OnPush` [change detection strategy](https://angular.io/api/core/ChangeDetectionStrategy).

**Why?** Enforces good practices such as immutability.

**Why?** Improves performance.

**Do** let Angular control change detection implicitly using `@Inputs()` reference changes, `@Outputs()` emitted events,
and the `async` pipe.

**Avoid** triggering change detection explicitly using `ApplicationRef.tick()`, `ChangeDetectorRef.detectChanges()`,
`NgZone` or any other manual methods.

**Do** mark object properties as `readonly` to enforce immutability.

```ts
export interface Hero {
  readonly id: number;
  readonly name: string;
}
```

**Why?** `OnPush` change detection do not work with mutable objects.

### Separate between presentational and container components

**Do** separate components into presentational and container components (aka. smart and dumb components).

## Presentational components

### Expose data structure through a model

**Do** create a model for each presentational component.

**Do** place the component module in the same named folder as the component.

```text
|- hero-list
|  |- hero-list.component.ts|html|css|spec.ts
|  |- hero-list.model.ts
```

**Do** match the name of the model with its component and remove the `Component` suffix.

```ts
export type HeroList = HeroListItem[];

export interface HeroListItem {
  readonly name: string;
  readonly alterEgo: string;
}
```

**Why?** Models shared by multiples components lead to mixed dependencies and eventual breaking changes.

**Why?** By exposing their own data structures, components are unlikely to be affected by outside changes.

### Delegate to the parent container

**Avoid** calculated properties in component models. For example, if a component displays a hero real name (aka. alter
ego), its model should contain a property `name` instead of having two properties `firstName` and `lastName`.

**Avoid** injecting store/services into presentational components.

**Why?** presentational components are responsible of displaying data. Any other logics are handled by container
components.

### Communicate through inputs and outputs

**Do** communicate with presentational components through `@Inputs()` and `@Outputs()`.

**Avoid** communicating with presentational components through `@ViewChild()`, `@ViewChildren()` or any equivalent
methods.

### Split into child components

**Do** split large presentational components into smaller ones.

**Do** place child components directly under their parent.

```text
|- hero-about
|  |- hero-power-list
|  |  |- hero-power-list.component.{ts,html,css,spec.ts}
|  |  |- hero-power-list.model.ts
|  |  |- hero-power-list.module.ts
|  |- hero-weakness-list
|  |  |- ...
|  |- hero-about.component.{ts,html,css,spec.ts}
|  |- hero-about.model.ts
|  |- hero-about.module.ts
```

**Consider** composing the component model with models from child presentational components.

```ts
import { HeroPowerList } from './hero-power-list/hero-power-list.model';
import { HeroWeaknessList } from './hero-weakness-list/hero-weakness-list.model';

export interface HeroAbout {
  readonly powers: HeroPowerList;
  readonly weaknesses: HeroWeaknessList;
}
```

## Container components

### Interact with the store/service

**Do** interact with the store (or service if no state management) in container components.

**Do** pass data returned by the store (or service if no state management into child presentational components using the
`async` pipe.

```ts
@Component({
  selector: 'toh-heroes',
  template: `<toh-hero-list [model]="heroes$ | async"></toh-hero-list>`,
})
export class HeroesContainer {
  heroes$: Observable<HeroList>;

  constructor(private store: Store<HeroSlice>) {
    this.heroes = this.store.select(HeroSelectors.all);
  }
}
```

**Why?** Hides `Observables<>` complexity from presentational components.

**Do** handle events emitted by presentational components to dispatch actions (or invoke service functions if no state
management).

```ts
@Component({
  selector: 'toh-heroes',
  template: `<toh-hero-list (remove)="onRemove($event)"></toh-hero-list>`,
})
export class HeroesContainer implements OnInit {
  onRemove(hero: HeroListItem) {
    if (!hero) return;
    this.store.dispatch(HeroActions.remove({ id: hero.id }));
  }
}
```

### Map to presentational models

**Do** map types returned by the store/service into models exposed by child presentational components from inside the
container component.

```ts
@Component({
  selector: 'toh-hero-dashboard',
  template: `<toh-hero-list [model]="heroes$ | async"></toh-hero-list>`,
})
export class HeroDashboardContainer {
  heroes$: Observable<HeroList>;

  constructor(private store: Store<HeroSlice>) {
    this.heroes = this.store.select(HeroSelectors.all).pipe(
      map(heroes =>
        heroes.map(hero => ({
          ...hero,
          alterEgo: `${hero.alterEgo?.firstName} ${hero.alterEgo?.lastName}`,
        })),
      ),
    );
  }
}
```

**Why?** This way, presentational models and store models are completely independent.

### Pre-load the store with guards

**Do** Pre-load the store by dispatching load actions from a guard.

```ts
@Injectable({ providedIn: 'root' })
export class HeroDetailGuard implements CanActivate {
  constructor(private store: Store<HeroSlice>) {}

  canActivate(route: ActivatedRouteSnapshot): Observable<boolean> {
    const { slug } = route.params;
    return this.store.select(HeroSelectors.selected).pipe(
      // dispatches only if the hero is not already loaded
      tap(hero => hero?.id === id || this.store.dispatch(HeroActions.loadOne({ slug }))),
      // waits until the hero is loaded (optional)
      filter(hero => !!hero),
      // navigate to the route
      switchMap(() => true),
      // otherwise, something went wrong
      catchError(() => of(false)),
    );
  }
}
```

**Avoid** dispatching actions from `ngOnInit()`.

```ts
/* avoid */

@Component({})
export class HeroDetailContainer implements OnInit {
  constructor(private store: Store<HeroSlice>, private route: ActivatedRoute) {}

  ngOnInit() {
    const { slug } = this.route.snapshot.params;
    this.store.dispatch(HeroActions.loadOne({ slug }));
  }
}
```

**Why?** Guards open the possibility to wait until the data is loaded before navigating.

**Why?** Adding routing dependencies to components makes them harder to test.

## Models

### Use functionless interfaces

**Do** define funtionless models using interfaces.

```ts
export class Hero {
  readonly id: number;
  readonly name: string;
}
```

**Why?** Functions cannot be serialized/deserialized meaning that all functions from types comming out of the store or
an HTTP request will be undefined.

**Why?** Functional programming encourage separating the data from the behavior.

## RxJS

### Prefer pure operators

**Avoid** unwrapping `Observables<>`.

```ts
/* avoid */

@Component(/*...*/)
export class HeroDetailComponent {
  readonly hero$: Observable<Hero>;
  private readonly hero: Hero;

  constructor(private store: Store<HeroSlice>) {
    this.hero$ = this.store.select(HeroSelectors.selected).pipe(tap(hero => (this.hero = hero)));
  }

  onRemoveWeakness(weakness: WeaknessListItem) {
    this.store.dispatch(HeroActions.removeWeakness({ heroId: this.hero.id, weaknessId: weakness.id }));
  }
}
```

**Do** prefer [pure](https://en.wikipedia.org/wiki/pure_function) RxJS operators.

```ts
@Component(/*...*/)
export class HeroDetailComponent {
  readonly hero$: Observable<Hero>;

  constructor(private store: Store<HeroSlice>) {
    this.hero$ = this.store.select(HeroSelectors.selected);
  }

  onRemoveWeakness(weakness: WeaknessListItem) {
    this.hero$
      .pipe(take(1))
      .subscribe(hero => this.store.dispatch(HeroActions.removeWeakness({ heroId: hero.id, weaknessId: weakness.id })));
  }
}
```

**Why?** Side effects make the code harder to read and debug.

### Unsubscribe

**Avoid** subscribing as much as possible using the `async` pipe or by returning an `Observable<>`.

**Do** unsubscribe using `take(1)` and/or `takeUntil()` after each `subscribe()`.

```ts
@Component(/*...*/)
export class HeroRegistrationFormComponent implements OnInit, OnDestroy {
  readonly form: FormGroup;
  private readonly destroy$ = new Subject<void>();

  constructor(builder: FormBuilder) {
    this.form = builder.group(/*...*/);
  }

  ngOnInit() {
    this.form
      .get('name')
      .valueChanges.pipe(
        filter(name => !!name),
        // unsubscribes once the component is destroyed.
        takeUntil(this.destroy$),
      )
      // autofills the slug textbox while the name is filled by the user.
      .subscribe(name => this.form.get('slug').setValue(this.toSlug(name)));
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private toSlug(name) {
    /*...*/
  }
}
```

<!-- ## State management

### Divide in modules

**Do** divide the store in independent sub-modules.

```text
|- store
|  |- hero
|  |  |- hero.{actions,effects,reducer,selectors}.ts
|  |  |- hero.model.ts
|  |  |- hero.module.ts
|  |  |- hero.service.ts
|  |  |- index.ts
|  |- vilain
|  |  |- ...
|  |- store.module.ts
```

**Why?** Helps define parts of the store that should be kept independent. -->

<!-- TODO: routing -->

---

### Related

- [A Comprehensive Guide to Angular `OnPush` Change Detection Strategy](https://netbasal.com/a-comprehensive-guide-to-angular-onpush-change-detection-strategy-5bac493074a4)
  by Netanel Basal
- [Pre-loading ngrx store with Route Guards](https://ultimatecourses.com/blog/preloading-ngrx-store-route-guards)
