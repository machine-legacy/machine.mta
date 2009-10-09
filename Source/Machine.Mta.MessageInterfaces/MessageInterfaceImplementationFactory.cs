using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Machine.Mta.MessageInterfaces
{
  public abstract class MessageInterfaceImplementationFactory<T> : IMessageInterfaceImplementationFactory
  {
    static readonly string AssemblyName = "Messages";
    private AssemblyBuilder _assemblyBuilder;
    private ModuleBuilder _moduleBuilder;

    public IEnumerable<KeyValuePair<Type, Type>> ImplementMessageInterfaces(IEnumerable<Type> types, params Type[] extraInterfacesToAlwaysInclude)
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

    private Type ImplementMessage(Type type, params Type[] extraInterfacesToAlwaysInclude)
    {
      string newTypeName = MakeImplementationName(type);
      TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Serializable;
      TypeBuilder typeBuilder = _moduleBuilder.DefineType(newTypeName, attributes);
      typeBuilder.AddInterfaceImplementation(type);
      foreach (var extraInterface in extraInterfacesToAlwaysInclude)
      {
        typeBuilder.AddInterfaceImplementation(extraInterface);
      }
      T state = ImplementMessage(typeBuilder, type, Properties(type));
      foreach (PropertyInfo property in Properties(type))
      {
        ImplementProperty(typeBuilder, property, state);
      }
      return typeBuilder.CreateType();
    }

    protected virtual IEnumerable<PropertyInfo> Properties(Type type)
    {
      foreach (Type interfaceType in MessageTypeHelpers.TypesToGenerateForType(type))
      {
        foreach (PropertyInfo property in interfaceType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
          if (!property.CanRead)
          {
            throw new InvalidOperationException(type.FullName + "." + property.Name + " property needs getter");
          }
          yield return property;
        }
      }
    }

    protected abstract T ImplementMessage(TypeBuilder typeBuilder, Type type, IEnumerable<PropertyInfo> properties);

    protected abstract void ImplementProperty(TypeBuilder typeBuilder, PropertyInfo property, T state);

    private static string MakeImplementationName(Type type)
    {
      return type.Namespace + ".__Impl." + type.Name;
    }
  }

  public interface IMessageInterfaceImplementationFactory
  {
    IEnumerable<KeyValuePair<Type, Type>> ImplementMessageInterfaces(IEnumerable<Type> types, params Type[] extraInterfacesToAlwaysInclude);
  }
}