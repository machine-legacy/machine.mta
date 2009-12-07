using System;
using System.Collections.Generic;
using NServiceBus;

namespace Machine.Mta
{
  public interface IMessageBus : IDisposable
  {
    void Start();
    void Send<T>(params T[] messages) where T : IMessage;
    void Send<T>(EndpointAddress destination, params T[] messages) where T : IMessage;
    void Send<T>(EndpointAddress destination, string correlationId, params T[] messages) where T : IMessage;
    void Send(EndpointAddress destination, MessagePayload payload);
    void SendLocal<T>(params T[] messages) where T : IMessage;
    void Stop();
    IRequestReplyBuilder Request<T>(params T[] messages) where T : IMessage;
    void Reply<T>(params T[] messages) where T : IMessage;
    void Publish<T>(params T[] messages) where T : IMessage;
  }

  public interface IRequestReplyBuilder
  {
    void OnReply(AsyncCallback callback, object state);
    void OnReply(AsyncCallback callback);
  }
}
