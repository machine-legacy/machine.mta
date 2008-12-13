using System;

namespace Machine.Mta.Sagas
{
  public interface ISagaHandler
  {
    ISagaState State
    {
      get;
      set;
    }
    Type SagaStateType
    {
      get;
    }
    void Complete();
  }
  public abstract class Saga<T> : ISagaHandler where T : ISagaState
  {
    ISagaState ISagaHandler.State
    {
      get { return this.State; }
      set { this.State = (T)value; }
    }
    
    public T State
    {
      get;
      protected set;
    }

    public Type SagaStateType
    {
      get { return typeof(T); }
    }

    public virtual void Complete()
    {
    }
  }
}