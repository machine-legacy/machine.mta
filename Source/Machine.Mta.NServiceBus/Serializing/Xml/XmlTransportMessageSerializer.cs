using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Machine.Mta.Serializing.Xml
{
  public class XmlTransportMessageSerializer : ITransportMessageSerializer
  {
    readonly XmlMessageSerializer _serializer;
    readonly MessageMapper _mapper;
    readonly IMessageRegisterer _registrar;

    public XmlTransportMessageSerializer(MessageMapper messageMapper, IMessageRegisterer messageRegisterer, XmlMessageSerializer serializer)
    {
      _mapper = messageMapper;
      _registrar = messageRegisterer;
      _serializer = serializer;
    }

    public void Initialize()
    {
      _mapper.Initialize(_registrar.MessageTypes);
      _serializer.MessageMapper = _mapper;
      _serializer.MessageTypes = _registrar.MessageTypes.ToList();
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