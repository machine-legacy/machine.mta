using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public static class BatchMessages
  {
    public static MessageBatcher<T> BatchedSend<T>(this IMessageBus bus, short size) where T : class, IMessage
    {
      return new MessageBatcher<T>(size, (b) => bus.Send(b));
    }

    public static MessageBatcher<T> BatchedPublish<T>(this IMessageBus bus, short size) where T : class, IMessage
    {
      return new MessageBatcher<T>(size, (b) => bus.Publish(b));
    }

    public static MessageBatcher<T> BatchedSendLocal<T>(this IMessageBus bus, short size) where T : class, IMessage
    {
      return new MessageBatcher<T>(size, (b) => bus.SendLocal(b));
    }

    public static MessageBatcher<T> BatchedSendTo<T>(this IMessageBus bus, EndpointAddress address, short size) where T : class, IMessage
    {
      return new MessageBatcher<T>(size, (b) => bus.Send(address, b));
    }
  }

  public class MessageBatcher<T> : IDisposable where T : class, IMessage
  {
    readonly List<T> _messages = new List<T>();
    readonly short _batchSize;
    readonly Action<T[]> _batch;

    public MessageBatcher(short batchSize, Action<T[]> batch)
    {
      _batch = batch;
      _batchSize = batchSize;
    }

    public void Add(T message)
    {
      _messages.Add(message);
      if (_messages.Count == _batchSize)
      {
        Dispose();
      }
    }

    public void Dispose()
    {
      _batch(_messages.ToArray());
      _messages.Clear();
    }
  }
}
