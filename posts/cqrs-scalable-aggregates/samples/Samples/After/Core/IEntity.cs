namespace Samples.After
{
  public interface IEntity { }

  public interface IEntity<TId> : IEntity
  {
    TId Id { get; } 
  }
}
