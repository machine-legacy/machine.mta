using System;
using System.Collections.Generic;

namespace Machine.Mta.Dispatching
{
  public class HandlerInvocation
  {
    readonly IMessage _message;
    readonly Type _messageType;
    readonly Type _handlerType;
    readonly object _handler;
    readonly Queue<IMessageAspect> _aspects;
    readonly IConsume<IMessage> _invoker;

    public IMessage Message
    {
      get { return _message; }
    }

    public Type MessageType
    {
      get { return _messageType; }
    }

    public Type HandlerType
    {
      get { return _handlerType; }
    }

    public object Handler
    {
      get { return _handler; }
    }

    public log4net.ILog HandlerLogger
    {
      get { return log4net.LogManager.GetLogger("Machine.Mta.Sagas.Handlers." + _handlerType.FullName); }
    }

    public HandlerInvocation(IMessage message, Type messageType, Type handlerType, object handler, IConsume<IMessage> invoker, Queue<IMessageAspect> aspects)
    {
      _message = message;
      _aspects = aspects;
      _messageType = messageType;
      _handlerType = handlerType;
      _handler = handler;
      _invoker = invoker;
    }

    public void Continue()
    {
      if (_aspects.Count > 0)
      {
        _aspects.Dequeue().Continue(this);
      }
      else
      {
        _invoker.Consume(_message);
      }
    }
  }
}