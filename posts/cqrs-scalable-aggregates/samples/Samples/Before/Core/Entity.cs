namespace Samples.Before
{
  public abstract class Entity<TSelf, TId>
    where TSelf : Entity<TSelf, TId>
  {
    protected Entity(TId id) => Id = id;

    public TId Id { get; }
  }
}
