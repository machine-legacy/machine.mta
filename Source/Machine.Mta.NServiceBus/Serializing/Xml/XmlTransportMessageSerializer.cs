using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Machine.Mta.Serializing.Xml
{
  public class XmlTransportMessageSerializer : ITransportMessageSerializer
  {
    readonly XmlMessageSerializer _serializer;
    readonly MtaMessageMapper _messageMapper;
    readonly IMessageRegisterer _messageRegisterer;

    public XmlTransportMessageSerializer(MtaMessageMapper messageMapper, IMessageRegisterer messageRegisterer, XmlMessageSerializer serializer)
    {
      _messageMapper = messageMapper;
      _messageRegisterer = messageRegisterer;
      _serializer = serializer;
    }

    public void Initialize()
    {
      _serializer.MessageMapper = _messageMapper;
      _serializer.MessageTypes = _messageRegisterer.MessageTypes.ToList();
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