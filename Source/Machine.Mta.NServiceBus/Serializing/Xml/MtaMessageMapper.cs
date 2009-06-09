using System;

using NServiceBus.MessageInterfaces;

namespace Machine.Mta.Serializing.Xml
{
  public class MtaMessageMapper : IMessageMapper
  {
    readonly IMessageRegisterer _registerer;
    readonly IMessageInterfaceImplementationsLookup _lookup;
    readonly IMessageFactory _factory;

    public MtaMessageMapper(IMessageInterfaceImplementationsLookup lookup, IMessageFactory factory, IMessageRegisterer registerer)
    {
      _lookup = lookup;
      _registerer = registerer;
      _factory = factory;
    }

    public T CreateInstance<T>() where T : NServiceBus.IMessage
    {
      return (T)_factory.Create(typeof(T));
    }

    public T CreateInstance<T>(Action<T> action) where T : NServiceBus.IMessage
    {
      T message = (T)_factory.Create(typeof(T));
      action(message);
      return message;
    }

    public object CreateInstance(Type messageType)
    {
      return _factory.Create(messageType);
    }

    public void Initialize(params Type[] types)
    {
    }

    public Type GetMappedTypeFor(Type type)
    {
      if (type.IsClass)
      {
        var mappedType = _lookup.GetInterfaceFor(type);
        if (mappedType != null)
          return mappedType;
        return type;
      }
      return _lookup.GetClassFor(type);
    }

    public Type GetMappedTypeFor(string typeName)
    {
      foreach (var type in _registerer.MessageTypes)
      {
        if (type.FullName == typeName)
          return type;
      }
      return Type.GetType(typeName);
    }
  }
}