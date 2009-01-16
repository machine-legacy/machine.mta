using System;
using System.Collections.Generic;

namespace Machine.Mta.InterfacesAsMessages
{
  public class MessageInterfaceImplementations
  {
    readonly Dictionary<Type, Type> _interfaceToClass = new Dictionary<Type, Type>();
    readonly Dictionary<Type, Type> _classToInterface = new Dictionary<Type, Type>();
    readonly MessageInterfaceImplementationFactory _messageInterfaceImplementationFactory;

    public MessageInterfaceImplementations(MessageInterfaceImplementationFactory messageInterfaceImplementationFactory)
    {
      _messageInterfaceImplementationFactory = messageInterfaceImplementationFactory;
    }

    public void GenerateImplementationsOf(params Type[] types)
    {
      GenerateImplementationsOf(new List<Type>(types));
    }

    public void GenerateImplementationsOf(IEnumerable<Type> types)
    {
      foreach (KeyValuePair<Type, Type> generated in _messageInterfaceImplementationFactory.GenerateImplementationsOf(types))
      {
        _interfaceToClass[generated.Key] = generated.Value;
        _classToInterface[generated.Value] = generated.Key;
      }
    }

    public Type GetClassFor(Type type)
    {
      return _interfaceToClass[type];
    }

    public Type GetInterfaceFor(Type type)
    {
      return _classToInterface[type];
    }

    public bool IsClassOrInterface(Type type)
    {
      return _interfaceToClass.ContainsKey(type) || _classToInterface.ContainsKey(type);
    }
  }
}