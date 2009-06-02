using System;

namespace Machine.Mta.Timing
{
  [Serializable]
  public class ScheduledPublish
  {
    readonly Guid _id;
    readonly DateTime _publishAt;
    readonly MessagePayload _messagePayload;
    readonly EndpointAddress[] _addresses;
    readonly Guid[] _sagaIds;

    public Guid Id
    {
      get { return _id; }
    }

    public DateTime PublishAt
    {
      get { return _publishAt; }
    }

    public MessagePayload MessagePayload
    {
      get { return _messagePayload; }
    }

    public EndpointAddress[] Addresses
    {
      get { return _addresses; }
    }

    public Guid[] SagaIds
    {
      get { return _sagaIds; }
    }

    public ScheduledPublish(DateTime publishAt, MessagePayload messagePayload, EndpointAddress[] addresses, Guid[] sagaIds)
    {
      _id = Guid.NewGuid();
      _publishAt = publishAt;
      _messagePayload = messagePayload;
      _addresses = addresses;
      _sagaIds = sagaIds;
    }
  }
}