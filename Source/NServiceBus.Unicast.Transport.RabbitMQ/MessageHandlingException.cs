using System;
using System.Runtime.Serialization;

namespace NServiceBus.Unicast.Transport.RabbitMQ
{
  public class MessageHandlingException : Exception
  {
    readonly Exception _startedProcessingException;
    readonly Exception _receivingException;
    readonly Exception _finishedProcessingException;

    public Exception StartedProcessingException
    {
      get { return _startedProcessingException; }
    }

    public Exception ReceivingException
    {
      get { return _receivingException; }
    }

    public Exception FinishedProcessingException
    {
      get { return _finishedProcessingException; }
    }

    public MessageHandlingException()
    {
    }

    public MessageHandlingException(string message, Exception startedProcessingException, Exception receivingException, Exception finishedProcessingException)
      : base(message)
    {
      _startedProcessingException = startedProcessingException;
      _receivingException = receivingException;
      _finishedProcessingException = finishedProcessingException;
    }

    public MessageHandlingException(string message)
      : base(message)
    {
    }

    public MessageHandlingException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

    protected MessageHandlingException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }
}