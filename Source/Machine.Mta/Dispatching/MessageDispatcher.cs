using System;
using System.Collections.Generic;

using Machine.Container.Services;

namespace Machine.Mta.Dispatching
{
  public class MessageDispatcher : IMessageDispatcher
  {
    readonly IMachineContainer _container;
    readonly IMessageAspectsProvider _messageAspectsProvider;
    readonly HandlerDiscoverer _handlerDiscoverer;

    public MessageDispatcher(IMachineContainer container, IMessageAspectsProvider messageAspectsProvider, IProvideHandlerTypes handlerTypes)
    {
      _container = container;
      _messageAspectsProvider = messageAspectsProvider;
      _handlerDiscoverer = new HandlerDiscoverer(container, handlerTypes);
    }

    private void Dispatch(IMessage message)
    {
      bool noHandlers = true;
      foreach (MessageHandlerType messageHandlerType in _handlerDiscoverer.GetHandlerTypesFor(message.GetType()))
      {
        noHandlers = false;
        Logging.Dispatch(message, messageHandlerType.TargetType);
        object handler = _container.Resolve.Object(messageHandlerType.TargetType);
        IConsume<IMessage> invoker = Invokers.CreateForHandler(messageHandlerType.TargetExpectsMessageOfType, handler);
        HandlerInvocation invocation = messageHandlerType.ToInvocation(message, handler, invoker, _messageAspectsProvider.DefaultAspects());
        invocation.Continue();
        if (CurrentMessageContext.Current.AskedToStopDispatchingCurrentMessageToHandlers)
        {
          break;
        }
      }
      if (noHandlers)
      {
        Logging.NoHandlersInDispatch(message);
      }
    }

    public void Dispatch(IMessage[] messages)
    {
      ForAllScopes(mm => mm.Begin());
      try
      {
        foreach (IMessage message in messages)
        {
          Dispatch(message);
        }
      }
      finally
      {
        ForAllScopes(mm => mm.End());
      }
    }

    void ForAllScopes(Action<IMessageModule> action)
    {
      foreach (var mm in _container.Resolve.All<IMessageModule>())
      {
        action(mm);
      }
    }
  }

  public static class InvocationMappings
  {
    public static HandlerInvocation ToInvocation(this MessageHandlerType messageHandlerType, IMessage message, object handler, IConsume<IMessage> invoker, Queue<IMessageAspect> aspects)
    {
      LastMessageAspect lastAspect = new LastMessageAspect(invoker, message);
      aspects.Enqueue(lastAspect);
      return new HandlerInvocation(message, messageHandlerType.TargetExpectsMessageOfType, messageHandlerType.TargetType, handler, invoker, aspects);
    }
  }
}