using System;
using log4net;

namespace Machine.Mta
{
  public static class Logging
  {
    static readonly string _loggerPrefix = "Machine.Mta.Messages";
    static readonly ILog _sendingLog = LogManager.GetLogger(_loggerPrefix + ".Sending");
    static readonly ILog _receivingLog = LogManager.GetLogger(_loggerPrefix + ".Receiving");
    static readonly ILog _poisonLog = LogManager.GetLogger(_loggerPrefix + ".Poison");
    static readonly ILog _dispatcherLog = LogManager.GetLogger(_loggerPrefix + ".Dispatching");
    static readonly ILog _errorLog = LogManager.GetLogger("Machine.Mta.Errors");

    public static void Publish(IMessage[] messages)
    {
      if (messages.Length == 0) return;
      string message = "Publishing " + messages[0].GetType() + " x " + messages.Length;
      _sendingLog.Info(message);
      ForMessage(messages[0]).Info(message);
    }

    public static void SendMessagePayload(EndpointAddress destination, MessagePayload message)
    {
      _sendingLog.Info("Sending Payload " + message + " to " + destination);
    }

    public static void Reply(IMessage[] messages)
    {
      if (messages.Length == 0) return;
      string message = "Replying " + messages[0].GetType() + " x " + messages.Length;
      _sendingLog.Info(message);
      ForMessage(messages[0]).Info(message);
    }

    public static void Send(EndpointAddress destination, IMessage[] messages)
    {
      if (messages.Length == 0) return;
      string message = "Sending " + messages[0].GetType() + " to " + destination + " x " + messages.Length;
      _sendingLog.Info(message);
      ForMessage(messages[0]).Info(message);
    }

    public static void Send(IMessage[] messages)
    {
      if (messages.Length == 0) return;
      string message = "Sending " + messages[0].GetType() + " x " + messages.Length;
      _sendingLog.Info(message);
      ForMessage(messages[0]).Info(message);
    }

    public static void NoHandlersInDispatch(IMessage message)
    {
      _dispatcherLog.Info("No handlers for " + message);
      ForMessage(message).Info("No handlers for " + message);
    }

    public static void Dispatch(IMessage message, Type handlerType)
    {
      _dispatcherLog.Info("Dispatching " + message + " to " + handlerType);
      ForMessage(message).Info("Dispatching " + message + " to " + handlerType);
    }

    public static void Poison(TransportMessage message)
    {
      _poisonLog.Info("Poison " + message.ReturnAddress + " CorrelationId=" + message.CorrelationId + " Id=" + message.Id);
    }

    public static void Received(TransportMessage message)
    {
      _receivingLog.Info("Receiving " + message.ReturnAddress + " CorrelationId=" + message.CorrelationId + " Id=" + message.Id);
    }

    public static void Error(Exception error)
    {
      _errorLog.Error(error);
    }

    private static ILog ForMessage(IMessage message)
    {
      return LogManager.GetLogger(_loggerPrefix + ".All." + message.GetType().Name);
    }
  }
}