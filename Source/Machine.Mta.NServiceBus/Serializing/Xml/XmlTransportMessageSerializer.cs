using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NServiceBus.MessageInterfaces;

namespace Machine.Mta.Serializing.Xml
{
  public class XmlTransportMessageSerializer : ITransportMessageSerializer
  {
    readonly IMessageMapper _mapper;
    readonly IMessageRegisterer _registrar;
    readonly XmlMessageSerializer _serializer;

    public XmlTransportMessageSerializer(IMessageMapper mapper, IMessageRegisterer registerer, XmlMessageSerializer serializer)
    {
      _mapper = mapper;
      _registrar = registerer;
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