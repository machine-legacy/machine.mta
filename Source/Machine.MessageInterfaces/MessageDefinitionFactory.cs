using System;
using System.Collections.Generic;
using System.Reflection;

namespace Machine.Mta.MessageInterfaces
{
  public class MessageDefinition
  {
    readonly Type _messageType;
    readonly List<MessageProperty> _properties = new List<MessageProperty>();

    public Type MessageType
    {
      get { return _messageType; }
    }

    public MessageDefinition(Type messageType)
    {
      _messageType = messageType;
    }

    public void AddProperty(string name, Type type, bool readOnly)
    {
      _properties.Add(new MessageProperty(name, type, readOnly));
    }

    public IEnumerable<MessagePropertyError> VerifyDictionaryAndReturnMissingProperties(IDictionary<string, object> dictionary)
    {
      var given = new List<string>(dictionary.Keys);
      foreach (var property in _properties)
      {
        given.Remove(property.Name);
        if (!dictionary.ContainsKey(property.Name))
        {
          yield return new MessagePropertyError(property.Name, MessagePropertyErrorType.Missing);
        }
      }
      foreach (var name in given)
      {
        yield return new MessagePropertyError(name, MessagePropertyErrorType.Extra);
      }
    }
  }
  
  public enum MessagePropertyErrorType
  {
    Missing,
    Extra
  }
  
  public class MessagePropertyError
  {
    readonly string _name;
    readonly MessagePropertyErrorType _type;

    public string Name
    {
      get { return _name; }
    }

    public MessagePropertyErrorType Type
    {
      get { return _type; }
    }

    public MessagePropertyError(string name, MessagePropertyErrorType type)
    {
      _name = name;
      _type = type;
    }
  }
  
  public class MessageProperty
  {
    readonly string _name;
    readonly Type _type;
    readonly bool _readOnly;

    public string Name
    {
      get { return _name; }
    }

    public Type Type
    {
      get { return _type; }
    }

    public bool ReadOnly
    {
      get { return _readOnly; }
    }

    public MessageProperty(string name, Type type, bool readOnly)
    {
      _name = name;
      _readOnly = readOnly;
      _type = type;
    }
  }
  
  public class MessageDefinitionFactory
  {
    public MessageDefinition CreateDefinition(Type messageType)
    {
      var definition = new MessageDefinition(messageType);
      foreach (var type in MessageTypeHelpers.TypesToGenerateForType(messageType))
      {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
          if (!property.CanRead)
          {
            throw new InvalidOperationException();
          }
          definition.AddProperty(property.Name, property.PropertyType, property.CanWrite);
        }
      }
      return definition;
    }
  }
}