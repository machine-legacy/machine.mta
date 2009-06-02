using System;
using System.Collections.Generic;
using NServiceBus;

namespace Machine.Mta
{
  public class BusProxy : IBus
  {
    public IBus CurrentBus()
    {
      return null;
    }

    public T CreateInstance<T>() where T : NServiceBus.IMessage
    {
      return CurrentBus().CreateInstance<T>();
    }

    public T CreateInstance<T>(Action<T> action) where T : NServiceBus.IMessage
    {
      return CurrentBus().CreateInstance<T>(action);
    }

    public object CreateInstance(Type messageType)
    {
      return CurrentBus().CreateInstance(messageType);
    }

    public void Publish<T>(params T[] messages) where T : NServiceBus.IMessage
    {
      CurrentBus().Publish(messages);
    }

    public void Publish<T>(Action<T> messageConstructor) where T : NServiceBus.IMessage
    {
      CurrentBus().Publish(messageConstructor);
    }

    public void Subscribe(Type messageType)
    {
      CurrentBus().Subscribe(messageType);
    }

    public void Subscribe<T>() where T : NServiceBus.IMessage
    {
      CurrentBus().Subscribe<T>();
    }

    public void Subscribe(Type messageType, Predicate<NServiceBus.IMessage> condition)
    {
      CurrentBus().Subscribe(messageType, condition);
    }

    public void Subscribe<T>(Predicate<T> condition) where T : NServiceBus.IMessage
    {
      CurrentBus().Subscribe<T>(condition);
    }

    public void Unsubscribe(Type messageType)
    {
      CurrentBus().Unsubscribe(messageType);
    }

    public void Unsubscribe<T>() where T : NServiceBus.IMessage
    {
      CurrentBus().Unsubscribe<T>();
    }

    public void SendLocal(params NServiceBus.IMessage[] messages)
    {
      CurrentBus().SendLocal(messages);
    }

    public void SendLocal<T>(Action<T> messageConstructor) where T : NServiceBus.IMessage
    {
      CurrentBus().SendLocal<T>(messageConstructor);
    }

    public ICallback Send(params NServiceBus.IMessage[] messages)
    {
      return CurrentBus().Send(messages);
    }

    public ICallback Send<T>(Action<T> messageConstructor) where T : NServiceBus.IMessage
    {
      return CurrentBus().Send<T>(messageConstructor);
    }

    public ICallback Send(string destination, params NServiceBus.IMessage[] messages)
    {
      return CurrentBus().Send(destination, messages);
    }

    public ICallback Send<T>(string destination, Action<T> messageConstructor) where T : NServiceBus.IMessage
    {
      return CurrentBus().Send(destination, messageConstructor);
    }

    public void Send(string destination, string correlationId, params NServiceBus.IMessage[] messages)
    {
      CurrentBus().Send(destination, correlationId, messages);
    }

    public void Send<T>(string destination, string correlationId, Action<T> messageConstructor) where T : NServiceBus.IMessage
    {
      CurrentBus().Send(destination, correlationId, messageConstructor);
    }

    public void Reply(params NServiceBus.IMessage[] messages)
    {
      CurrentBus().Reply(messages);
    }

    public void Reply<T>(Action<T> messageConstructor) where T : NServiceBus.IMessage
    {
      CurrentBus().Reply<T>(messageConstructor);
    }

    public void Return(int errorCode)
    {
      CurrentBus().Return(errorCode);
    }

    public void HandleCurrentMessageLater()
    {
      CurrentBus().HandleCurrentMessageLater();
    }

    public void DoNotContinueDispatchingCurrentMessageToHandlers()
    {
      CurrentBus().DoNotContinueDispatchingCurrentMessageToHandlers();
    }

    public IDictionary<string, string> OutgoingHeaders
    {
      get { return CurrentBus().OutgoingHeaders; }
    }

    public IMessageContext CurrentMessageContext
    {
      get { return CurrentBus().CurrentMessageContext; }
    }
  }
}
