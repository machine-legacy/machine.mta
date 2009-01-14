using System;

namespace Machine.Mta
{
  [Serializable]
  public class TransportMessage
  {
    private readonly EndpointName _returnAddress;
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

    public EndpointName ReturnAddress
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

    protected TransportMessage(Guid id, EndpointName returnAddress, Guid correlationId, Guid returnCorrelationId, Guid[] sagaIds, byte[] body, string label)
    {
      _id = id;
      _returnAddress = returnAddress;
      _correlationId = correlationId;
      _returnCorrelationId = returnCorrelationId;
      _sagaIds = sagaIds;
      _body = body;
      _label = label;
    }

    public override string ToString()
    {
      return this.Label + " from " + _returnAddress + " with " + _body.Length + "bytes";
    }

    public static TransportMessage For(EndpointName returnAddress, Guid correlationId, Guid returnCorrelationId, Guid[] sagaIds, MessagePayload payload)
    {
      Guid id = Guid.NewGuid();
      if (returnCorrelationId == Guid.Empty)
      {
        returnCorrelationId = id;
      }
      return new TransportMessage(id, returnAddress, correlationId, returnCorrelationId, sagaIds, payload.ToByteArray(), payload.Label);
    }
  }
}