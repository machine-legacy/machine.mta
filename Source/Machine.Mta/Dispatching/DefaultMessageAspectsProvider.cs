using System;
using System.Collections.Generic;

using Machine.Container.Services;
using Machine.Mta.Sagas;

namespace Machine.Mta.Dispatching
{
  public class DefaultMessageAspectsProvider : IMessageAspectsProvider
  {
    readonly IMachineContainer _container;

    public DefaultMessageAspectsProvider(IMachineContainer container)
    {
      _container = container;
    }

    protected virtual Type[] AspectTypes
    {
      get { return new[] { typeof(SagaAspect) }; }
    }

    public Queue<IMessageAspect> DefaultAspects()
    {
      Queue<IMessageAspect> aspects = new Queue<IMessageAspect>();
      foreach (Type type in this.AspectTypes)
      {
        aspects.Enqueue((IMessageAspect)_container.Resolve.Object(type));
      }
      return aspects;
    }
  }
}