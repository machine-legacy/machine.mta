using System;
using System.IO;
using NServiceBus;

namespace Machine.Mta
{
  public interface ITransportMessageSerializer
  {
    void Serialize(IMessage[] messages, Stream stream);
    IMessage[] Deserialize(Stream stream);
  }
}