using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using NServiceBus;

namespace Machine.Mta.Testing
{
  public class MockMessageBus : IMessageBus
  {
    class MessageSentToDestination
    {
      public EndpointAddress Address { get; set; }
      public IMessage Message { get; set; }
    }

    readonly List<IMessage> _messagesPublished = new List<IMessage>();
    readonly List<IMessage> _replies = new List<IMessage>();
    readonly List<IMessage> _requests = new List<IMessage>();
    readonly List<MessageSentToDestination> _messagesSent = new List<MessageSentToDestination>();
    readonly EndpointAddress _localAddress = NameAndHostAddress.ForLocalQueue(Guid.NewGuid().ToString()).ToAddress();
    readonly EndpointAddress _poisonAddress = NameAndHostAddress.ForLocalQueue(Guid.NewGuid().ToString()).ToAddress();

    public IEnumerable<IMessage> SentMessages()
    {
      return _messagesSent.Select(x => x.Message);
    }

    public IEnumerable<IMessage> PublishedMessages()
    {
      return _messagesPublished;
    }

    public void Dispose()
    {
    }

    public void Start()
    {
    }

    public void Send<T>(params T[] messages) where T : IMessage
    {
      Send(EndpointAddress.Null, messages);
    }

    public void Send<T>(string correlationId, params T[] messages) where T : IMessage
    {
      _messagesSent.AddRange(messages.Select(m => new MessageSentToDestination { Message = m }));
    }

    public void Send<T>(EndpointAddress destination, params T[] messages) where T : IMessage
    {
      _messagesSent.AddRange(messages.Select(m => new MessageSentToDestination { Address = destination, Message = m }));
    }

    public void Send(EndpointAddress destination, MessagePayload payload)
    {
      throw new NotImplementedException();
    }

    public void SendLocal<T>(params T[] messages) where T : IMessage
    {
      Send(_localAddress, messages);
    }

    public void Stop()
    {
    }

    public IRequestReplyBuilder Request<T>(params T[] messages) where T : IMessage
    {
      AddMessagesTo(_requests, messages);
      return new MockRequestReplyBuilder();
    }

    public IRequestReplyBuilder Request<T>(string correlationId, params T[] messages) where T : IMessage
    {
      AddMessagesTo(_requests, messages);
      return new MockRequestReplyBuilder();
    }

    public void Reply<T>(params T[] messages) where T : IMessage
    {
      AddMessagesTo(_replies, messages);
    }

    public void Send<T>(EndpointAddress destination, string correlationId, params T[] messages) where T : IMessage
    {
      AddMessagesTo(_replies, messages);
    }

    static void AddMessagesTo<T>(ICollection<IMessage> messageList, IEnumerable<T> messages) where T: IMessage
    {
      foreach (var message in messages)
      {
        messageList.Add(message);
      }
    }

    public void Publish<T>(params T[] messages) where T : IMessage
    {
      AddMessagesTo(_messagesPublished, messages);
    }

    public EndpointAddress PoisonAddress
    {
      get { return _poisonAddress; }
    }

    public EndpointAddress Address
    {
      get { return _localAddress; }
    }
    public void ShouldNotHavePublished<T>(T message) where T : IMessage
    {
      ShouldNotHavePublished<T>(m => m.Equals(message));
    }

    public void ShouldNotHavePublished<T>() where T : IMessage
    {
      ShouldNotHavePublished<T>(m => true);
    }

    public void ShouldNotHavePublished<T>(Func<T, bool> filter) where T : IMessage
    {
      MessageListShouldNotHave(_messagesPublished, filter);
    }

    public void ShouldHavePublished<T>(T message) where T : IMessage
    {
      ShouldHavePublished<T>(m => m.Equals(message));
    }

    public void ShouldHavePublished<T>() where T : IMessage
    {
      ShouldHavePublished<T>(m => true);
    }

    public void ShouldHavePublished<T>(Func<T, bool> filter) where T : IMessage
    {
      MessageListShouldHave(_messagesPublished, filter);
    }

    public void ShouldHaveRequested<T>(T message) where T : IMessage
    {
      ShouldHaveRequested<T>(m => m.Equals(message));
    }

    public void ShouldHaveRequested<T>() where T : IMessage
    {
      ShouldHaveRequested<T>(m => true);
    }

    public void ShouldHaveRequested<T>(Func<T, bool> filter) where T : IMessage
    {
      MessageListShouldHave(_requests, filter);
    }

    public void ShouldNotHaveRequested<T>(T message) where T : IMessage
    {
      ShouldNotHaveRequested<T>(m => m.Equals(message));
    }

    public void ShouldNotHaveRequested<T>() where T : IMessage
    {
      ShouldNotHaveRequested<T>(m => true);
    }

    public void ShouldNotHaveRequested<T>(Func<T, bool> filter) where T : IMessage
    {
      MessageListShouldNotHave(_requests, filter);
    }

    public void ShouldHaveReplied<T>(T message) where T : IMessage
    {
      ShouldHaveReplied<T>(m => m.Equals(message));
    }

    public void ShouldHaveReplied<T>() where T : IMessage
    {
      ShouldHaveReplied<T>(m => true);
    }

    public void ShouldHaveReplied<T>(Func<T, bool> filter) where T : IMessage
    {
      MessageListShouldHave(_replies, filter);
    }

    public void ShouldNotHaveReplied<T>(T message) where T : IMessage
    {
      ShouldNotHaveReplied<T>(m => m.Equals(message));
    }

    public void ShouldNotHaveReplied<T>() where T : IMessage
    {
      ShouldNotHaveReplied<T>(m => true);
    }

    public void ShouldNotHaveReplied<T>(Func<T, bool> filter) where T : IMessage
    {
      MessageListShouldNotHave(_replies, filter);
    }

    public void ShouldHaveSent<T>(Func<T, bool> filter) where T : IMessage
    {
      var sentMessages = _messagesSent.Select(m => m.Message);
      MessageListShouldHave(sentMessages, filter);
    }

    public void ShouldHaveSent<T>() where T : IMessage
    {
      ShouldHaveSent<T>(m => true);
    }

    public void ShouldHaveSent<T>(T message) where T : IMessage
    {
      ShouldHaveSent<T>(m => message.Equals(m));
    }

    public void ShouldNotHaveSent<T>(Func<T, bool> filter) where T : IMessage
    {
      var sentMessages = _messagesSent.Select(m => m.Message);
      MessageListShouldNotHave(sentMessages, filter);
    }

    public void ShouldNotHaveSent<T>() where T : IMessage
    {
      ShouldNotHaveSent<T>(m => true);
    }

    public void ShouldNotHaveSent<T>(T message) where T : IMessage
    {
      ShouldNotHaveSent<T>(m => message.Equals(m));
    }

    public void ShouldHaveSentTo<T>(EndpointAddress address, Func<T, bool> filter)
      where T : IMessage
    {
      var messagesSentToAddress = _messagesSent.Where(m => m.Address != EndpointAddress.Null).Where(m => address.Equals(m.Address)).Select(m => m.Message);
      MessageListShouldHave(messagesSentToAddress, filter);
    }

    public void ShouldHaveSentTo<T>(EndpointAddress address) where T : IMessage
    {
      ShouldHaveSentTo<T>(address, m => true);
    }

    public void ShouldHaveSentTo<T>(EndpointAddress address, T message) where T : IMessage
    {
      ShouldHaveSentTo<T>(address, m => message.Equals(m));
    }

    public void ShouldNotHaveSentTo<T>(EndpointAddress address, Func<T, bool> filter)
      where T : IMessage
    {
      var messagesSentToAddress = _messagesSent.Where(m => address.Equals(m.Address)).Select(m => m.Message);
      MessageListShouldNotHave(messagesSentToAddress, filter);
    }

    public void ShouldNotHaveSentTo<T>(EndpointAddress address) where T : IMessage
    {
      ShouldHaveSentTo<T>(address, m => true);
    }

    public void ShouldNotHaveSentTo<T>(EndpointAddress address, T message) where T : IMessage
    {
      ShouldNotHaveSentTo<T>(address, m => message.Equals(m));
    }

    static void MessageListShouldHave<T>(IEnumerable<IMessage> messageList, Func<T, bool> filter)
      where T : IMessage
    {
      messageList.OfType<T>().Where(filter).ShouldNotBeEmpty();
    }

    static void MessageListShouldNotHave<T>(IEnumerable<IMessage> messageList, Func<T, bool> filter)
      where T : IMessage
    {
      messageList.OfType<T>().Where(filter).ShouldBeEmpty();
    }

    public IEnumerable<T> MessagesSentOfType<T>() where T : IMessage
    {
      return _messagesSent.Select(messageSent => messageSent.Message).OfType<T>();
    }

    public void Reset()
    {
      _messagesSent.Clear();
      _messagesPublished.Clear();
      _replies.Clear();
      _requests.Clear();
    }
  }

  internal class MockRequestReplyBuilder : IRequestReplyBuilder
  {
    public void OnReply(AsyncCallback callback, object state)
    {
      callback(null);
    }

    public void OnReply(AsyncCallback callback)
    {
      callback(null);
    }
  }
}
