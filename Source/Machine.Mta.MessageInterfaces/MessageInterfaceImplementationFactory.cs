using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Machine.Mta.MessageInterfaces
{
  public abstract class MessageInterfaceImplementationFactory<T> : IMessageInterfaceImplementationFactory
  {
    readonly static string AssemblyName = "Messages";
    private AssemblyBuilder _assemblyBuilder;
    private ModuleBuilder _moduleBuilder;

    public IEnumerable<KeyValuePair<Type, Type>> ImplementMessageInterfaces(IEnumerable<Type> types, params Type[] extraInterfacesToAlwaysInclude)
    {
      var assemblyName = new AssemblyName(AssemblyName);
      _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
      _moduleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName, AssemblyName + ".dll");
      foreach (var type in types)
      {
        if (!type.IsInterface)
        {
          throw new InvalidOperationException(type + " is NOT and interface!");
        }
        var generatedType = ImplementMessage(type);
        yield return new KeyValuePair<Type, Type>(type, generatedType);
      }
    }

    private Type ImplementMessage(Type type, params Type[] extraInterfacesToAlwaysInclude)
    {
      var newTypeName = MakeImplementationName(type);
      var attributes = TypeAttributes.Public | TypeAttributes.Serializable;
      var typeBuilder = _moduleBuilder.DefineType(newTypeName, attributes);
      typeBuilder.AddInterfaceImplementation(type);
      foreach (var extraInterface in extraInterfacesToAlwaysInclude)
      {
        typeBuilder.AddInterfaceImplementation(extraInterface);
      }
      T state = ImplementMessage(typeBuilder, type, Properties(type));
      foreach (var property in Properties(type))
      {
        ImplementProperty(typeBuilder, property, state);
      }
      return typeBuilder.CreateType();
    }

    protected virtual IEnumerable<PropertyInfo> Properties(Type type)
    {
      foreach (var interfaceType in MessageTypeHelpers.TypesToGenerateForType(type))
      {
        foreach (var property in interfaceType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
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