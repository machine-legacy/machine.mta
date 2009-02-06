using System;
using System.Collections.Generic;

namespace Machine.Mta.Dispatching
{
  public class MessageHandlerType
  {
    readonly Type _targetType;
    readonly Type _consumerType;

    public Type TargetType
    {
      get { return _targetType; }
    }

    public Type ConsumerType
    {
      get { return _consumerType; }
    }

    public Type TargetExpectsMessageOfType
    {
      get { return _consumerType.GetGenericArguments()[0]; }
    }

    public MessageHandlerType(Type targetType, Type consumerType)
    {
      _targetType = targetType;
      _consumerType = consumerType;
    }

    public override string ToString()
    {
      return "Invoke " + this.TargetType.FullName + " to handle " + this.TargetExpectsMessageOfType.FullName;
    }
  }
}