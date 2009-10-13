using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Machine.Mta.Serializing.Xml
{
  public class XmlTransportMessageBodyFormatter : ITransportMessageBodyFormatter
  {
    readonly XmlMessageSerializer _serializer;
    readonly MtaMessageMapper _messageMapper;

    public XmlTransportMessageBodyFormatter(MtaMessageMapper messageMapper)
    {
      _messageMapper = messageMapper;
      _serializer = new XmlMessageSerializer();
    }

    public void Initialize()
    {
      _serializer.MessageMapper = _messageMapper;
    }

    public void Serialize(IMessage[] messages, Stream stream)
    {
      _serializer.Serialize(messages.Cast<NServiceBus.IMessage>().ToArray(), stream);
    }

    public IMessage[] Deserialize(Stream stream)
    {
      return _serializer.Deserialize(stream).Cast<Machine.Mta.IMessage>().ToArray();
    }
  }
}