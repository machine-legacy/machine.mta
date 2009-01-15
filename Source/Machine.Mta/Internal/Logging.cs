using System;
using log4net;

namespace Machine.Mta.Internal
{
  public static class Logging
  {
    static readonly string _loggerPrefix = "Machine.Mta.Messages";
    static readonly ILog _sendingLog = LogManager.GetLogger(_loggerPrefix + ".Sending");
    static readonly ILog _receivingLog = LogManager.GetLogger(_loggerPrefix + ".Receiving");
    static readonly ILog _poisonLog = LogManager.GetLogger(_loggerPrefix + ".Poison");
    static readonly ILog _dispatcherLog = LogManager.GetLogger(_loggerPrefix + ".Dispatching");

    public static void Publish(IMessage[] messages)
    {
      foreach (IMessage message in messages)
      {
        _sendingLog.Info("Publishing " + message);
        ForMessage(message).Info("Publishing " + message);
      }
    }

    public static void SendMessagePayload(EndpointName destination, MessagePayload message)
    {
      _sendingLog.Info("Sending Payload " + message + " to " + destination);
    }

    public static void Reply(IMessage[] messages)
    {
      foreach (IMessage message in messages)
      {
        _sendingLog.Info("Publishing " + message);
        ForMessage(message).Info("Publishing " + message);
      }
    }

    public static void Send(EndpointName destination, IMessage[] messages)
    {
      foreach (IMessage message in messages)
      {
        _sendingLog.Info("Sending " + message + " to " + destination);
        ForMessage(message).Info("Sending " + message + " to " + destination);
      }
    }

    public static void Send(IMessage[] messages)
    {
      foreach (IMessage message in messages)
      {
        _sendingLog.Info("Sending " + message);
        ForMessage(message).Info("Sending " + message);
      }
    }

    public static void Dispatch(IMessage message)
    {
      _dispatcherLog.Info("Dispatching " + message);
      ForMessage(message).Info("Dispatching " + message);
    }

    public static void Poison(TransportMessage message)
    {
      _poisonLog.Info("Poison " + message.ReturnAddress + " CorrelationId=" + message.CorrelationId + " Id=" + message.Id);
    }

    public static void Received(TransportMessage message)
    {
      _receivingLog.Info("Receiving " + message.ReturnAddress + " CorrelationId=" + message.CorrelationId + " Id=" + message.Id);
    }

    private static ILog ForMessage(IMessage message)
    {
      return LogManager.GetLogger(_loggerPrefix + ".All." + message.GetType().Name);
    }
  }
}