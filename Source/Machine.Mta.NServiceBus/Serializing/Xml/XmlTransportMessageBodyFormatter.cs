using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Machine.Mta.Serializing.Xml
{
  public class XmlTransportMessageBodyFormatter : ITransportMessageBodyFormatter
  {
    readonly IMessageRegisterer _registerer;
    readonly XmlMessageSerializer _serializer;
    readonly MtaMessageMapper _messageMapper;

    public XmlTransportMessageBodyFormatter(IMessageRegisterer registerer, MtaMessageMapper messageMapper)
    {
      _registerer = registerer;
      _messageMapper = messageMapper;
      _serializer = new XmlMessageSerializer();
    }

    public void Initialize()
    {
      _serializer.MessageMapper = _messageMapper;
      _serializer.Initialize(_registerer.MessageTypes.ToArray());
    }

    public void Serialize(IMessage[] messages, Stream stream)
    {
      _serializer.Serialize(messages, stream);
    }

    public IMessage[] Deserialize(Stream stream)
    {
      return _serializer.Deserialize(stream).Cast<Machine.Mta.IMessage>().ToArray();
    }
  }
}