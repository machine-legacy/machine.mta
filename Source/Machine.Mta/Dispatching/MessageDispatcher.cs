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
      Logging.Dispatch(message);
      foreach (MessageHandlerType messageHandlerType in _handlerDiscoverer.GetHandlerTypesFor(message.GetType()))
      {
        object handler = _container.Resolve.Object(messageHandlerType.TargetType);
        IConsume<IMessage> invoker = Invokers.CreateForHandler(messageHandlerType.TargetExpectsMessageOfType, handler);
        HandlerInvocation invocation = messageHandlerType.ToInvocation(message, handler, invoker, _messageAspectsProvider.DefaultAspects());
        invocation.Continue();
        if (CurrentMessageContext.Current.AskedToStopDispatchingCurrentMessageToHandlers)
        {
          break;
        }
      }
    }

    public void Dispatch(IMessage[] messages)
    {
      foreach (IMessage message in messages)
      {
        Dispatch(message);
      }
    }
  }

  public static class InvocationMappings
  {
    public static HandlerInvocation ToInvocation(this MessageHandlerType messageHandlerType, IMessage message, object handler, IConsume<IMessage> invoker, Queue<IMessageAspect> aspects)
    {
      return new HandlerInvocation(message, messageHandlerType.TargetExpectsMessageOfType, messageHandlerType.TargetType, handler, invoker, aspects);
    }
  }
}