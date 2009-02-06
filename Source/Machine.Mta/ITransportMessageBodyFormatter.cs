using System;
using System.IO;

namespace Machine.Mta
{
  public interface ITransportMessageBodyFormatter
  {
    void Serialize(IMessage[] messages, Stream stream);
    IMessage[] Deserialize(Stream stream);
  }
}