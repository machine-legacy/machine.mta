using System;
using System.Collections.Generic;
using System.IO;
using NServiceBus;

namespace Machine.Mta
{
  public class MessagePayloadSerializer
  {
    private readonly ITransportMessageSerializer _transportMessageSerializer;

    public MessagePayloadSerializer(ITransportMessageSerializer transportMessageSerializer)
    {
      _transportMessageSerializer = transportMessageSerializer;
    }

    public MessagePayload Serialize<T>(params T[] messages) where T : IMessage
    {
      using (var stream = new MemoryStream())
      {
        var nonGeneric = new IMessage[messages.Length];
        Array.Copy(messages, nonGeneric, nonGeneric.Length);
        _transportMessageSerializer.Serialize(nonGeneric, stream);
        return new MessagePayload(stream.ToArray());
      }
    }

    public IMessage[] Deserialize(MessagePayload payload)
    {
      using (var stream = new MemoryStream(payload.ToByteArray()))
      {
        var messages = new List<IMessage>();
        messages.AddRange(_transportMessageSerializer.Deserialize(stream));
        return messages.ToArray();
      }
    }
  }
}
