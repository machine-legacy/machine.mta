using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Machine.Mta.InterfacesAsMessages
{
  public class MessageInterfaceImplementationFactory
  {
    private AssemblyBuilder _assemblyBuilder;
    private ModuleBuilder _moduleBuilder;

    public IEnumerable<KeyValuePair<Type, Type>> GenerateImplementationsOf(IEnumerable<Type> types)
    {
      string name = "Messages";
      AssemblyName assemblyName = new AssemblyName();
      assemblyName.Name = name;
      _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
      _moduleBuilder = _assemblyBuilder.DefineDynamicModule(name, name + ".dll");
      foreach (Type type in types)
      {
        if (!type.IsInterface)
        {
          throw new InvalidOperationException();
        }
        Type generatedType = GenerateStub(type);
        yield return new KeyValuePair<Type, Type>(type, generatedType);
      }
    }

    private Type GenerateStub(Type type)
    {
      string newTypeName = MakeImplementationName(type);
      TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Serializable;
      TypeBuilder typeBuilder = _moduleBuilder.DefineType(newTypeName, attributes);
      typeBuilder.AddInterfaceImplementation(type);
      foreach (Type interfaceType in MessageTypeHelpers.TypesToGenerateForType(type))
      {
        foreach (PropertyInfo property in interfaceType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
          if (!property.CanRead || !property.CanWrite)
          {
            throw new InvalidOperationException("Bad message type: " + type.FullName + " property needs getter and setter: " + property.Name);
          }
          FieldBuilder fieldBuilder = typeBuilder.DefineField(MakeFieldName(property), property.PropertyType, FieldAttributes.Private);
          PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, new Type[0]);
          MethodBuilder getMethod = typeBuilder.DefineMethod("get_" + propertyBuilder.Name, MethodAttributes.Virtual | MethodAttributes.Public, property.PropertyType, new Type[0]);
          ILGenerator ilGet = getMethod.GetILGenerator();
          ilGet.Emit(OpCodes.Ldarg_0);
          ilGet.Emit(OpCodes.Ldfld, fieldBuilder);
          ilGet.Emit(OpCodes.Ret);

          propertyBuilder.SetGetMethod(getMethod);
          typeBuilder.DefineMethodOverride(getMethod, property.GetGetMethod());

          MethodBuilder setMethod = typeBuilder.DefineMethod("set_" + propertyBuilder.Name, MethodAttributes.Virtual | MethodAttributes.Public, typeof(void), new Type[] { property.PropertyType });
          ParameterBuilder parameterBuilder = setMethod.DefineParameter(0, ParameterAttributes.None, "value");
          ILGenerator ilSet = setMethod.GetILGenerator();
          ilSet.Emit(OpCodes.Ldarg_0);
          ilSet.Emit(OpCodes.Ldarg_1);
          ilSet.Emit(OpCodes.Stfld, fieldBuilder);
          ilSet.Emit(OpCodes.Ret);

          propertyBuilder.SetSetMethod(setMethod);
          typeBuilder.DefineMethodOverride(setMethod, property.GetSetMethod());
        }
      }
      return typeBuilder.CreateType();
    }

    private static string MakeFieldName(PropertyInfo property)
    {
      return "_" + property.Name;
    }

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