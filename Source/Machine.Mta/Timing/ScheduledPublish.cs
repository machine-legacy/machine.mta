using System;

namespace Machine.Mta.Timing
{
  [Serializable]
  public class ScheduledPublish
  {
    readonly Guid _id;
    readonly DateTime _publishAt;
    readonly byte[] _messagePayload;
    readonly string[] _addresses;
    readonly Guid[] _sagaIds;

    public Guid Id
    {
      get { return _id; }
    }

    public DateTime PublishAt
    {
      get { return _publishAt; }
    }

    public byte[] MessagePayload
    {
      get { return _messagePayload; }
    }

    public string[] Addresses
    {
      get { return _addresses; }
    }

    public Guid[] SagaIds
    {
      get { return _sagaIds; }
    }

    public ScheduledPublish(DateTime publishAt, byte[] messagePayload, string[] addresses, Guid[] sagaIds)
    {
      _id = Guid.NewGuid();
      _publishAt = publishAt;
      _messagePayload = messagePayload;
      _addresses = addresses;
      _sagaIds = sagaIds;
    }
  }
}