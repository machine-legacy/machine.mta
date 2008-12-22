using System;
using System.IO;

namespace Machine.Mta.Internal
{
  public interface ITransportMessageBodyFormatter
  {
    void Serialize(IMessage[] messages, Stream stream);
    IMessage[] Deserialize(Stream stream);
  }
}