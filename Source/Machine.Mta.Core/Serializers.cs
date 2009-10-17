using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

namespace Machine.Mta
{
  public class Serializers
  {
    static BinaryFormatter _binaryFormatter;

    static Serializers()
    { 
      _binaryFormatter = new BinaryFormatter();
      _binaryFormatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
      _binaryFormatter.Binder = new SerializationBinders(new TransportMessageBinder(), new GeneratedMessageTypeBinder());
    }

    public static BinaryFormatter Binary
    {
      get { return _binaryFormatter; }
      set { _binaryFormatter = value; }
    }
  }

  public class TransportMessageBinder : SerializationBinder
  {
    public override Type BindToType(string assemblyName, string typeName)
    {
      if (typeName == typeof(TransportMessage).FullName)
      {
        return typeof(TransportMessage);
      }
      if (typeName == typeof(EndpointAddress).FullName)
      {
        return typeof(EndpointAddress);
      }
      return null;
    }
  }

  public class GeneratedMessageTypeBinder : SerializationBinder
  {
    readonly string _messagesAssemblyName = "Messages, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
    Assembly _messagesAssembly;

    public override Type BindToType(string assemblyName, string typeName)
    {
      if (assemblyName != _messagesAssemblyName)
      {
        return null;
      }
      if (_messagesAssembly == null)
      {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
          if (assembly.FullName == _messagesAssemblyName)
          {
            _messagesAssembly = assembly;
          }
        }
        if (_messagesAssembly == null)
        {
          throw new InvalidOperationException("Unable to locate Messages assembly dynamically.");
        }
      }
      return _messagesAssembly.GetType(typeName);
    }
  }

  public class SerializationBinders : SerializationBinder
  {
    readonly SerializationBinder[] _binders;

    public SerializationBinders(params SerializationBinder[] binders)
    {
      _binders = binders;
    }

    public override Type BindToType(string assemblyName, string typeName)
    {
      foreach (SerializationBinder binder in _binders)
      {
        Type resolved = binder.BindToType(assemblyName, typeName);
        if (resolved != null)
        {
          return resolved;
        }
      }
      return null;
    }
  }
}
