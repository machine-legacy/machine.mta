using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Machine.Mta.InterfacesAsMessages
{
  public class GeneratedMessage
  {
    readonly Dictionary<string, FieldBuilder> _fields = new Dictionary<string, FieldBuilder>();

    public FieldBuilder this[string name]
    {
      get { return _fields[name]; }
    }

    public IEnumerable<KeyValuePair<string, FieldBuilder>> Fields
    {
      get { return _fields; }
    }

    public void AddField(string name, FieldBuilder field)
    {
      _fields[name] = field;
    }
  }

  public class DefaultMessageInterfaceImplementationFactory : MessageInterfaceImplementationFactory<GeneratedMessage>
  {
    protected override GeneratedMessage ImplementMessage(TypeBuilder typeBuilder, Type type, IEnumerable<PropertyInfo> properties)
    {
      GeneratedMessage generatedMessage = new GeneratedMessage();
      foreach (PropertyInfo property in properties)
      {
        FieldBuilder fieldBuilder = typeBuilder.DefineField(MakeFieldName(property), property.PropertyType, FieldAttributes.Private);
        generatedMessage.AddField(property.Name, fieldBuilder);
      }
      {
        ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
        ILGenerator il = ctorBuilder.GetILGenerator();
        il.Emit(OpCodes.Ret);
      }
      GenerateDictionaryConstructor(typeBuilder, generatedMessage);
      return generatedMessage;
    }

    protected static void GenerateDictionaryConstructor(TypeBuilder typeBuilder, GeneratedMessage generatedMessage)
    {
      MethodInfo dictionaryGetMethod = typeof(IDictionary<string, object>).GetMethod("get_Item");
      ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(IDictionary<string, object>) });
      ctorBuilder.DefineParameter(0, ParameterAttributes.None, "dictionary");
      ILGenerator il = ctorBuilder.GetILGenerator();
      foreach (KeyValuePair<string, FieldBuilder> property in generatedMessage.Fields)
      {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldstr, property.Key);
        il.Emit(OpCodes.Callvirt, dictionaryGetMethod);
        if (property.Value.FieldType.IsValueType)
        {
          il.Emit(OpCodes.Unbox_Any, property.Value.FieldType);
        }
        else
        {
          il.Emit(OpCodes.Castclass, property.Value.FieldType);
        }
        il.Emit(OpCodes.Stfld, generatedMessage[property.Key]);
      }
      il.Emit(OpCodes.Ret);
    }

    protected override void ImplementProperty(TypeBuilder typeBuilder, PropertyInfo property, GeneratedMessage state)
    {
      FieldBuilder fieldBuilder = state[property.Name];
      PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, new Type[0]);
      MethodBuilder getMethod = typeBuilder.DefineMethod("get_" + propertyBuilder.Name, MethodAttributes.Virtual | MethodAttributes.Public, property.PropertyType, new Type[0]);
      ILGenerator ilGet = getMethod.GetILGenerator();
      ilGet.Emit(OpCodes.Ldarg_0);
      ilGet.Emit(OpCodes.Ldfld, fieldBuilder);
      ilGet.Emit(OpCodes.Ret);

      propertyBuilder.SetGetMethod(getMethod);
      typeBuilder.DefineMethodOverride(getMethod, property.GetGetMethod());

      MethodBuilder setMethod = typeBuilder.DefineMethod("set_" + propertyBuilder.Name, MethodAttributes.Virtual | MethodAttributes.Public, typeof(void), new[] { property.PropertyType });
      setMethod.DefineParameter(0, ParameterAttributes.None, "value");
      ILGenerator ilSet = setMethod.GetILGenerator();
      ilSet.Emit(OpCodes.Ldarg_0);
      ilSet.Emit(OpCodes.Ldarg_1);
      ilSet.Emit(OpCodes.Stfld, fieldBuilder);
      ilSet.Emit(OpCodes.Ret);

      propertyBuilder.SetSetMethod(setMethod);
      if (property.GetSetMethod() != null)
      {
        typeBuilder.DefineMethodOverride(setMethod, property.GetSetMethod());
      }
    }

    private static string MakeFieldName(PropertyInfo property)
    {
      return "_" + property.Name;
    }
  }
}