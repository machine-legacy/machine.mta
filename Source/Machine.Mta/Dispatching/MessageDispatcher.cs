using System;
using System.Collections.Generic;

using Machine.Container.Services;

namespace Machine.Mta.Dispatching
{
  public class MessageDispatcher : IMessageDispatcher
  {
    readonly IMachineContainer _container;
    readonly HandlerDiscoverer _handlerDiscoverer;
    readonly IMessageAspectsProvider _messageAspectsProvider;

    public MessageDispatcher(IMachineContainer container, IMessageAspectsProvider messageAspectsProvider)
    {
      _container = container;
      _messageAspectsProvider = messageAspectsProvider;
      _handlerDiscoverer = new HandlerDiscoverer(container);
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