using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Mta.Dispatching;
using NServiceBus;

namespace Machine.Mta
{
  public static class HandlerAndMessageReflection
  {
    public static IEnumerable<HandlerAndMessageType> GetHandlerAndMessageTypes(IProvideHandlerTypes handlerTypes)
    {
      foreach (var handlerType in handlerTypes.HandlerTypes())
      {
        var handlerConsumes = handlerType.AllGenericVariations(typeof(IConsume<>)).Union(handlerType.AllGenericVariations(typeof(IMessageHandler<>)));
        foreach (var handlerConsume in handlerConsumes.Distinct())
        {
          yield return new HandlerAndMessageType {
            HandlerType = handlerType,
            MessageType = handlerConsume.GetGenericArguments().First()
          };
        }
      }
    }
  }

  public class HandlerAndMessageType
  {
    public Type HandlerType { get; set; }
    public Type MessageType { get; set; }
  }
}
