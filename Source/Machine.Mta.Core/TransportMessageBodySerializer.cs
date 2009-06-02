using System;
using System.Collections.Generic;
using System.IO;

namespace Machine.Mta
{
  public class TransportMessageBodySerializer
  {
    private readonly ITransportMessageBodyFormatter _transportMessageBodyFormatter;

    public TransportMessageBodySerializer(ITransportMessageBodyFormatter transportMessageBodyFormatter)
    {
      _transportMessageBodyFormatter = transportMessageBodyFormatter;
    }

    public MessagePayload Serialize<T>(params T[] messages) where T : IMessage
    {
      using (MemoryStream stream = new MemoryStream())
      {
        IMessage[] nonGeneric = new IMessage[messages.Length];
        Array.Copy(messages, nonGeneric, nonGeneric.Length);
        _transportMessageBodyFormatter.Serialize(nonGeneric, stream);
        return new MessagePayload(stream.ToArray(), typeof(T).Name);
      }
    }

    public IMessage[] Deserialize(byte[] body)
    {
      using (MemoryStream stream = new MemoryStream(body))
      {
        List<IMessage> messages = new List<IMessage>();
        messages.AddRange(_transportMessageBodyFormatter.Deserialize(stream));
        return messages.ToArray();
      }
    }
  }
}
