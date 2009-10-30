using System;
using System.Collections.Generic;

using NServiceBus.MessageInterfaces;

namespace Machine.Mta.MessageInterfaces
{
  public class MessageMapper : IMessageMapper
  {
    readonly static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageMapper));
    readonly MessageInterfaceImplementations implementations;
    readonly OpaqueMessageFactory _opaqueMessageFactory;
    IEnumerable<Type> _types;

    public MessageMapper()
    {
      implementations = new MessageInterfaceImplementations(new DefaultMessageInterfaceImplementationFactory());
      _opaqueMessageFactory = new OpaqueMessageFactory(implementations, new MessageDefinitionFactory());
    }

    public T CreateInstance<T>() where T : NServiceBus.IMessage
    {
      return (T)_opaqueMessageFactory.Create(typeof(T));
    }

    public T CreateInstance<T>(Action<T> action) where T : NServiceBus.IMessage
    {
      T message = (T)_opaqueMessageFactory.Create(typeof(T));
      action(message);
      return message;
    }

    public object CreateInstance(Type messageType)
    {
      return _opaqueMessageFactory.Create(messageType);
    }

    public void Initialize(IEnumerable<Type> types)
    {
      _types = types;
      _opaqueMessageFactory.Initialize(types);
    }

    public Type GetMappedTypeFor(Type type)
    {
      if (type.IsClass)
      {
        if (implementations.IsClassOrInterface(type))
        {
          return implementations.GetClassOrInterfaceFor(type);
        }
        return type;
      }
      if (implementations.IsClassOrInterface(type))
      {
        return implementations.GetClassOrInterfaceFor(type);
      }
      return null;
    }

    public Type GetMappedTypeFor(string typeName)
    {
      foreach (var type in _types)
      {
        if (type.FullName == typeName)
          return type;
      }
      var permutations = new[] {typeName};
      if (!typeName.Contains(","))
      {
        permutations = new[] { typeName, typeName + ", NServiceBus.Core" };
      }
      foreach (var permutation in permutations)
      {
        try
        {
          var found = Type.GetType(permutation);
          if (found != null)
          {
            return found;
          }
        }
        catch (Exception)
        {
          _log.Error("Error resolving: " + permutation);
          return null;
        }
      }
      return null;
    }
  }
}