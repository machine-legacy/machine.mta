using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Machine.Mta.Serializing.Xml
{
  public class XmlTransportMessageBodyFormatter : ITransportMessageBodyFormatter
  {
    readonly IMessageRegisterer _registerer;
    readonly MessageSerializer _serializer;
    readonly MtaMessageMapper _messageMapper;

    public XmlTransportMessageBodyFormatter(IMessageRegisterer registerer, MtaMessageMapper messageMapper)
    {
      _registerer = registerer;
      _messageMapper = messageMapper;
      _serializer = new MessageSerializer();
    }

    public void Initialize()
    {
      _serializer.MessageMapper = _messageMapper;
      _serializer.Initialize(_registerer.MessageTypes.ToArray());
    }

    public void Serialize(IMessage[] messages, Stream stream)
    {
      try
      {
        _serializer.Serialize(messages, stream);
      }
      catch (Exception error)
      {
        throw new InvalidOperationException("Error serializing: " + messages[0].GetType(), error);
      }
    }

    public IMessage[] Deserialize(Stream stream)
    {
      return _serializer.Deserialize(stream).Cast<Machine.Mta.IMessage>().ToArray();
    }
  }
}