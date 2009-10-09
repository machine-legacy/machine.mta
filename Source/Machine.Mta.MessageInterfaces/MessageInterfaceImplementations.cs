using System;
using System.Collections.Generic;
using System.Threading;

using Machine.Core.Utility;

namespace Machine.Mta.InterfacesAsMessages
{
  public class MessageInterfaceImplementations : IMessageInterfaceImplementationsLookup
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageInterfaceImplementations));
    readonly Dictionary<Type, Type> _interfaceToClass = new Dictionary<Type, Type>();
    readonly Dictionary<Type, Type> _classToInterface = new Dictionary<Type, Type>();
    readonly IMessageInterfaceImplementationFactory _messageInterfaceImplementationFactory;
    readonly IMessageRegisterer _registerer;
    readonly ReaderWriterLock _lock = new ReaderWriterLock();
    bool _generated;

    public MessageInterfaceImplementations(IMessageInterfaceImplementationFactory messageInterfaceImplementationFactory, IMessageRegisterer registerer)
    {
      _messageInterfaceImplementationFactory = messageInterfaceImplementationFactory;
      _registerer = registerer;
    }

    public Type GetClassFor(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        if (!_interfaceToClass.ContainsKey(type))
          throw new KeyNotFoundException(type.FullName);
        return _interfaceToClass[type];
      }
    }

    public Type GetInterfaceFor(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        if (!_classToInterface.ContainsKey(type))
          throw new KeyNotFoundException(type.FullName);
        return _classToInterface[type];
      }
    }

    public Type GetClassOrInterfaceFor(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        if (_interfaceToClass.ContainsKey(type))
          return _interfaceToClass[type];
        if (_classToInterface.ContainsKey(type))
          return _classToInterface[type];
        return null;
      }
    }

    public bool IsClassOrInterface(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        return _interfaceToClass.ContainsKey(type) || _classToInterface.ContainsKey(type);
      }
    }

    void GenerateIfNecessary()
    {
      if (RWLock.UpgradeToWriterIf(_lock, () => !_generated))
      {
        _log.Info("Generating");
        foreach (KeyValuePair<Type, Type> generated in _messageInterfaceImplementationFactory.ImplementMessageInterfaces(_registerer.MessageTypes))
        {
          _interfaceToClass[generated.Key] = generated.Value;
          _classToInterface[generated.Value] = generated.Key;
        }
        _generated = true;
      }
    }
  }
}