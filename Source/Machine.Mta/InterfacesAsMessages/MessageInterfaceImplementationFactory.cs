using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Machine.Mta.InterfacesAsMessages
{
  public interface IMessageInterfaceImplementationFactory
  {
    IEnumerable<KeyValuePair<Type, Type>> ImplementMessageInterfaces(IEnumerable<Type> types);
  }
  public abstract class MessageInterfaceImplementationFactory<T> : IMessageInterfaceImplementationFactory
  {
    static readonly string AssemblyName = "Messages";
    private AssemblyBuilder _assemblyBuilder;
    private ModuleBuilder _moduleBuilder;

    public IEnumerable<KeyValuePair<Type, Type>> ImplementMessageInterfaces(IEnumerable<Type> types)
    {
      AssemblyName assemblyName = new AssemblyName(AssemblyName);
      _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
      _moduleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName, AssemblyName + ".dll");
      foreach (Type type in types)
      {
        if (!type.IsInterface)
        {
          throw new InvalidOperationException();
        }
        Type generatedType = ImplementMessage(type);
        yield return new KeyValuePair<Type, Type>(type, generatedType);
      }
    }

    private Type ImplementMessage(Type type)
    {
      string newTypeName = MakeImplementationName(type);
      TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Serializable;
      TypeBuilder typeBuilder = _moduleBuilder.DefineType(newTypeName, attributes);
      typeBuilder.AddInterfaceImplementation(type);
      T state = ImplementMessage(typeBuilder, type);
      foreach (Type interfaceType in MessageTypeHelpers.TypesToGenerateForType(type))
      {
        foreach (PropertyInfo property in interfaceType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
          if (!property.CanRead)
          {
            throw new InvalidOperationException(type.FullName + "." + property.Name + " property needs getter");
          }
          ImplementProperty(typeBuilder, property, state);
        }
      }
      return typeBuilder.CreateType();
    }

    protected abstract T ImplementMessage(TypeBuilder typeBuilder, Type type);

    protected abstract void ImplementProperty(TypeBuilder typeBuilder, PropertyInfo property, T state);

    private static string MakeImplementationName(Type type)
    {
      string name = type.Name;
      if (name.StartsWith("I"))
      {
        name = name.Substring(1);
      }
      return "A" + name;
    }
  }
}