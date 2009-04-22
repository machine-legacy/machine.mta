using System;

namespace Machine.Mta
{
  [Serializable]
  public class TransportMessage
  {
    string _id;
    readonly EndpointAddress _returnAddress;
    readonly string _correlationId;
    readonly Guid[] _sagaIds;
    readonly byte[] _body;
    readonly string _label;

    public string Id
    {
      get { return _id; }
      set { _id = value; }
    }

    public string CorrelationId
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

    protected TransportMessage(string id, EndpointAddress returnAddress, string correlationId, Guid[] sagaIds, byte[] body, string label)
    {
      _id = id;
      _returnAddress = returnAddress;
      _correlationId = correlationId;
      _sagaIds = sagaIds;
      _body = body;
      _label = label;
    }

    public override string ToString()
    {
      return this.Label + " from " + _returnAddress + " with " + _body.Length + "bytes";
    }

    public static TransportMessage For(string id, EndpointAddress returnAddress, string correlationId, Guid[] sagaIds, MessagePayload payload)
    {
      return new TransportMessage(id, returnAddress, correlationId, sagaIds, payload.ToByteArray(), payload.Label);
    }

    public static TransportMessage For(EndpointAddress returnAddress, string correlationId, Guid[] sagaIds, MessagePayload payload)
    {
      return new TransportMessage(null, returnAddress, correlationId, sagaIds, payload.ToByteArray(), payload.Label);
    }
  }
}