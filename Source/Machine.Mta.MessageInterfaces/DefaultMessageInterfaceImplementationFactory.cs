using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Machine.Mta.MessageInterfaces
{
  public class GeneratedMessage
  {
    readonly Dictionary<string, FieldBuilder> _fields = new Dictionary<string, FieldBuilder>();
    readonly Type _messageType;

    public GeneratedMessage(Type messageType)
    {
      _messageType = messageType;
    }

    public Type MessageType
    {
      get { return _messageType; }
    }

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
      GeneratedMessage generatedMessage = new GeneratedMessage(type);
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
      GenerateEquals(typeBuilder, generatedMessage);
      GenerateGetHashCode(typeBuilder, generatedMessage);
      return generatedMessage;
    }

    protected void GenerateEquals(TypeBuilder typeBuilder, GeneratedMessage generatedMessage)
    {
      Type type = generatedMessage.MessageType;
      MethodInfo arrayEquals = typeof(ArrayHelpers).GetMethod("AreArraysEqual", new[] { typeof(Array), typeof(Array) });
      MethodInfo objectEquals = typeof(Object).GetMethod("Equals", new [] { typeof(Object), typeof(Object) });
      MethodBuilder method = typeBuilder.DefineMethod("Equals", MethodAttributes.Virtual | MethodAttributes.Public, typeof (bool), new[] { typeof (Object) });
      ILGenerator il = method.GetILGenerator();
      LocalBuilder local = il.DeclareLocal(type);
      il.Emit(OpCodes.Ldarg_1);
      il.Emit(OpCodes.Isinst, type);
      il.Emit(OpCodes.Stloc_0);
      il.Emit(OpCodes.Ldloc_0);
      ReturnIfNull(il, () => {
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ret);
      });
      foreach (var pair in generatedMessage.Fields)
      {
        FieldInfo field = pair.Value;
        Label label = il.DefineLabel();
        if (field.FieldType.IsArray)
        {
          il.Emit(OpCodes.Ldarg_0);
          il.Emit(OpCodes.Ldfld, field);
          il.Emit(OpCodes.Ldloc_0);
          il.Emit(OpCodes.Ldfld, field);
          il.Emit(OpCodes.Call, arrayEquals);
        }
        else if (field.FieldType.IsValueType && !field.FieldType.IsEnum && !field.FieldType.IsNullableType())
        {
          MethodInfo valueEquals = field.FieldType.GetMethod("Equals", new [] { field.FieldType });
          il.Emit(OpCodes.Ldarg_0);
          il.Emit(OpCodes.Ldflda, field);
          il.Emit(OpCodes.Ldloc_0);
          il.Emit(OpCodes.Ldfld, field);
          il.Emit(OpCodes.Callvirt, valueEquals);
        }
        else
        {
          il.Emit(OpCodes.Ldarg_0);
          il.Emit(OpCodes.Ldfld, field);
          if (field.FieldType.IsValueType)
          {
            il.Emit(OpCodes.Box, field.FieldType);
          }
          il.Emit(OpCodes.Ldloc_0);
          il.Emit(OpCodes.Ldfld, field);
          if (field.FieldType.IsValueType)
          {
            il.Emit(OpCodes.Box, field.FieldType);
          }
          il.Emit(OpCodes.Call, objectEquals);
        }
        il.Emit(OpCodes.Brtrue, label);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ret);
        il.MarkLabel(label);
      }
      il.Emit(OpCodes.Ldc_I4_1);
      il.Emit(OpCodes.Ret);
    }

    protected void GenerateGetHashCode(TypeBuilder typeBuilder, GeneratedMessage generatedMessage)
    {
      MethodInfo arrayGetHashCode = typeof(ArrayHelpers).GetMethod("GetHashCode", new[] { typeof(Array) });
      MethodBuilder method = typeBuilder.DefineMethod("GetHashCode", MethodAttributes.Virtual | MethodAttributes.Public, typeof(Int32), new Type[0]);
      ILGenerator il = method.GetILGenerator();

      bool hasState = false;
      foreach (var pair in generatedMessage.Fields)
      {
        FieldInfo field = pair.Value;
        MethodInfo getHashCode = field.FieldType.GetMethod("GetHashCode", new Type[0]);
        if (getHashCode == null)
        {
          getHashCode = typeof(Object).GetMethod("GetHashCode", new Type[0]);
        }
        il.Emit(OpCodes.Ldarg_0);
        if (field.FieldType.IsArray)
        {
          il.Emit(OpCodes.Ldfld, field);
          il.Emit(OpCodes.Call, arrayGetHashCode);
        }
        else if (field.FieldType.IsValueType)
        {
          if (field.FieldType.IsEnum)
          {
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Box, field.FieldType);
            il.Emit(OpCodes.Callvirt, getHashCode);
          }
          else
          {
            il.Emit(OpCodes.Ldflda, field);
            il.Emit(OpCodes.Call, getHashCode);
          }
        }
        else
        {
          il.Emit(OpCodes.Ldfld, field);
          IfNull(il, delegate() { il.Emit(OpCodes.Ldc_I4_0); }, delegate() {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Callvirt, getHashCode);
          });
        }
        if (hasState)
        {
          il.Emit(OpCodes.Ldc_I4, 29);
          il.Emit(OpCodes.Mul);
          il.Emit(OpCodes.Add);
        }
        hasState = true;
      }
      if (!hasState)
      {
        il.Emit(OpCodes.Ldc_I4_0);
      }
      il.Emit(OpCodes.Ret);
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
    
    private static void ReturnIfNull(ILGenerator il, Action ifNull)
    {
      Label nope = il.DefineLabel();
      il.Emit(OpCodes.Brtrue, nope);
      ifNull();
      il.MarkLabel(nope);
    }

    private static void IfNull(ILGenerator il, Action isTrue, Action isFalse)
    {
      Label nope = il.DefineLabel();
      Label done = il.DefineLabel();
      il.Emit(OpCodes.Ldnull);
      il.Emit(OpCodes.Ceq); // Has 1 if NULL
      il.Emit(OpCodes.Ldc_I4_0);
      il.Emit(OpCodes.Ceq); // Has 1 if NOT NULL
      il.Emit(OpCodes.Brtrue, nope);
      isTrue();
      il.Emit(OpCodes.Br, done);
      il.MarkLabel(nope);
      isFalse();
      il.MarkLabel(done);
    }
  }

  public static class TypeHelpers
  {
    public static bool IsNullableType(this Type type)
    {
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
    }
  }

  public class ArrayHelpers
  {
    public static bool AreArraysEqual(Array a1, Array a2)
    {
      if (a1 == null && a2 == null) return true;
      if (a1 == null || a2 == null) return false;
      if (a1.Length != a2.Length) return false;
      for (var i = 0; i < a1.Length; ++i)
      {
        if (!Equals(a1.GetValue(i), a2.GetValue(i)))
          return false;
      }
      return true;
    }

    public static Int32 GetHashCode(Array array)
    {
      if (array == null) return 0;
      Int32 code = 0;
      for (var i = 0; i < array.Length; ++i)
      {
        object value = array.GetValue(i);
        if (value != null)
        {
          code ^= value.GetHashCode();
        }
      }
      return code;
    }
  }
}