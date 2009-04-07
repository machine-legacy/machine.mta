using System;

namespace Machine.Mta
{
  [Serializable]
  public class TransportMessage
  {
    private readonly EndpointAddress _returnAddress;
    private readonly Guid _id;
    private readonly Guid _returnCorrelationId;
    private readonly Guid _correlationId;
    private readonly Guid[] _sagaIds;
    private readonly byte[] _body;
    private readonly string _label;

    public Guid Id
    {
      get { return _id; }
    }

    public Guid ReturnCorrelationId
    {
      get { return _returnCorrelationId; }
    }

    public Guid CorrelationId
    {
      get { return _correlationId; }
    }

    public Guid[] SagaIds
    {
      get { return _sagaIds; }
    }

    public EndpointAddress ReturnAddress
    {
      get { return _returnAddress; }
    }

    public byte[] Body
    {
      get { return _body; }
    }

    public string Label
    {
      get { return "TM<" + _label + ">"; }
    }

    protected TransportMessage()
    {
    }

    protected TransportMessage(Guid id, EndpointAddress returnAddress, Guid correlationId, Guid[] sagaIds, byte[] body, string label)
    {
      _id = id;
      _returnAddress = returnAddress;
      _correlationId = correlationId;
      _returnCorrelationId = Guid.Empty;
      _sagaIds = sagaIds;
      _body = body;
      _label = label;
    }

    public override string ToString()
    {
      return this.Label + " from " + _returnAddress + " with " + _body.Length + "bytes";
    }

    public static TransportMessage For(EndpointAddress returnAddress, Guid correlationId, Guid[] sagaIds, MessagePayload payload)
    {
      Guid id = Guid.NewGuid();
      if (correlationId == Guid.Empty)
      {
        correlationId = id;
      }
      return new TransportMessage(id, returnAddress, correlationId, sagaIds, payload.ToByteArray(), payload.Label);
    }
  }
}