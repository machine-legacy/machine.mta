using System;
using System.Collections.Generic;
using System.Threading;

using Machine.Core.Utility;

namespace Machine.Mta.InterfacesAsMessages
{
  public class MessageInterfaceImplementations
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageInterfaceImplementations));
    readonly List<Type> _messageTypes = new List<Type>();
    readonly Dictionary<Type, Type> _interfaceToClass = new Dictionary<Type, Type>();
    readonly Dictionary<Type, Type> _classToInterface = new Dictionary<Type, Type>();
    readonly IMessageInterfaceImplementationFactory _messageInterfaceImplementationFactory;
    readonly ReaderWriterLock _lock = new ReaderWriterLock();
    bool _generated;

    public MessageInterfaceImplementations(IMessageInterfaceImplementationFactory messageInterfaceImplementationFactory)
    {
      _messageInterfaceImplementationFactory = messageInterfaceImplementationFactory;
    }

    public void GenerateImplementationsOf(params Type[] types)
    {
      GenerateImplementationsOf(new List<Type>(types));
    }

    public void GenerateImplementationsOf(IEnumerable<Type> types)
    {
      using (RWLock.AsWriter(_lock))
      {
        if (_generated)
        {
          throw new InvalidOperationException("Message interfaces already implemented!");
        }
        foreach (Type type in types)
        {
          if (!_messageTypes.Contains(type))
          {
            _messageTypes.Add(type);
          }
        }
      }
    }

    public Type GetClassFor(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        return _interfaceToClass[type];
      }
    }

    public Type GetInterfaceFor(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        return _classToInterface[type];
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

    private void GenerateIfNecessary()
    {
      if (RWLock.UpgradeToWriterIf(_lock, () => !_generated))
      {
        _log.Info("Generating");
        foreach (KeyValuePair<Type, Type> generated in _messageInterfaceImplementationFactory.ImplementMessageInterfaces(_messageTypes))
        {
          _interfaceToClass[generated.Key] = generated.Value;
          _classToInterface[generated.Value] = generated.Key;
        }
        _generated = true;
      }
    }
  }
}