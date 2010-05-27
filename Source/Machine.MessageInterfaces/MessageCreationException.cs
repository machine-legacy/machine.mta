using System;
using System.Runtime.Serialization;

namespace Machine.Mta.MessageInterfaces
{
  [Serializable]
  public class MessageCreationException : Exception
  {
    public MessageCreationException()
    {
    }

    public MessageCreationException(string message) : base(message)
    {
    }

    public MessageCreationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected MessageCreationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}