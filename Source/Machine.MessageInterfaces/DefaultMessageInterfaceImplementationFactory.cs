using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

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
      var generatedMessage = new GeneratedMessage(type);
      foreach (var property in properties)
      {
        var fieldBuilder = typeBuilder.DefineField(MakeFieldName(property), property.PropertyType, FieldAttributes.Private);
        generatedMessage.AddField(property.Name, fieldBuilder);
      }
      {
        var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
        var il = ctorBuilder.GetILGenerator();
        il.Emit(OpCodes.Ret);
      }
      GenerateDictionaryConstructor(typeBuilder, generatedMessage);
      GenerateEquals(typeBuilder, generatedMessage);
      GenerateGetHashCode(typeBuilder, generatedMessage);
      GenerateToString(typeBuilder, generatedMessage);
      return generatedMessage;
    }

    protected void GenerateEquals(TypeBuilder typeBuilder, GeneratedMessage generatedMessage)
    {
      var type = generatedMessage.MessageType;
      var arrayEquals = typeof(ArrayHelpers).GetMethod("AreArraysEqual", new[] { typeof(Array), typeof(Array) });
      var objectEquals = typeof(Object).GetMethod("Equals", new [] { typeof(Object), typeof(Object) });
      var method = typeBuilder.DefineMethod("Equals", MethodAttributes.Virtual | MethodAttributes.Public, typeof (bool), new[] { typeof (Object) });
      var il = method.GetILGenerator();
      var local = il.DeclareLocal(type);
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
        var field = pair.Value;
        var label = il.DefineLabel();
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
          var valueEquals = field.FieldType.GetMethod("Equals", new [] { field.FieldType });
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

    protected void GenerateToString(TypeBuilder typeBuilder, GeneratedMessage generatedMessage)
    {
      var ctor = typeof(StringBuilder).GetConstructor(new Type[0]);
      var objectToString = typeof(Object).GetMethod("ToString");
      var appendObject = typeof(StringBuilder).GetMethod("Append", new[] { typeof(object) });
      var arrayToString = typeof(ArrayHelpers).GetMethod("ToString", new[] { typeof(Array) });
      var method = typeBuilder.DefineMethod("ToString", MethodAttributes.Virtual | MethodAttributes.Public, typeof(string), new Type[0]);
      var il = method.GetILGenerator();
      var local = il.DeclareLocal(typeof(StringBuilder));
      var first = true;

      il.Emit(OpCodes.Newobj, ctor);
      il.Emit(OpCodes.Stloc_0);
      il.Emit(OpCodes.Ldloc_0);
      il.Emit(OpCodes.Ldstr, generatedMessage.MessageType.Name + "<");
      il.Emit(OpCodes.Callvirt, appendObject);
      il.Emit(OpCodes.Pop);

      foreach (var pair in generatedMessage.Fields)
      {
        var field = pair.Value;
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldstr, (first ? "" : ", ") + pair.Key + "=");
        il.Emit(OpCodes.Callvirt, appendObject);
        il.Emit(OpCodes.Pop);

        var toString = field.FieldType.GetMethod("ToString", new Type[0]);
        if (toString == null)
        {
          toString = typeof(Object).GetMethod("ToString", new Type[0]);
        }
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldarg_0);
        if (field.FieldType.IsArray)
        {
          il.Emit(OpCodes.Ldfld, field);
          il.Emit(OpCodes.Call, arrayToString);
        }
        else if (field.FieldType.IsValueType)
        {
          if (field.FieldType.IsEnum)
          {
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Box, field.FieldType);
            il.Emit(OpCodes.Callvirt, toString);
          }
          else
          {
            if (field.FieldType.IsNullableType())
            {
              il.Emit(OpCodes.Ldflda, field);
              il.Emit(OpCodes.Call, field.FieldType.GetMethod("get_HasValue"));
              IfFalse(il, delegate() { il.Emit(OpCodes.Ldstr, "(null)"); }, delegate() {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, field);
                il.Emit(OpCodes.Callvirt, toString);
              });
            }
            else
            {
              il.Emit(OpCodes.Ldflda, field);
              il.Emit(OpCodes.Call, toString);
            }
          }
        }
        else
        {
          il.Emit(OpCodes.Ldfld, field);
          IfNull(il, delegate() { il.Emit(OpCodes.Ldstr, "(null)"); }, delegate() {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Callvirt, toString);
          });
        }
        il.Emit(OpCodes.Callvirt, appendObject);
        il.Emit(OpCodes.Pop);
        first = false;
      }

      il.Emit(OpCodes.Ldloc_0);
      il.Emit(OpCodes.Ldstr, ">");
      il.Emit(OpCodes.Callvirt, appendObject);
      il.Emit(OpCodes.Callvirt, objectToString);
      il.Emit(OpCodes.Ret);
    }

    protected void GenerateGetHashCode(TypeBuilder typeBuilder, GeneratedMessage generatedMessage)
    {
      var arrayGetHashCode = typeof(ArrayHelpers).GetMethod("GetHashCode", new[] { typeof(Array) });
      var method = typeBuilder.DefineMethod("GetHashCode", MethodAttributes.Virtual | MethodAttributes.Public, typeof(Int32), new Type[0]);
      var il = method.GetILGenerator();

      var hasState = false;
      foreach (var pair in generatedMessage.Fields)
      {
        var field = pair.Value;
        var getHashCode = field.FieldType.GetMethod("GetHashCode", new Type[0]);
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

    static void GenerateDictionaryConstructor(TypeBuilder typeBuilder, GeneratedMessage generatedMessage)
    {
      var dictionaryGetMethod = typeof(IDictionary<string, object>).GetMethod("get_Item");
      var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(IDictionary<string, object>) });
      ctorBuilder.DefineParameter(1, ParameterAttributes.None, "dictionary");
      var il = ctorBuilder.GetILGenerator();
      foreach (var property in generatedMessage.Fields)
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
      var fieldBuilder = state[property.Name];
      var propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, new Type[0]);
      var getMethod = typeBuilder.DefineMethod("get_" + propertyBuilder.Name, MethodAttributes.Virtual | MethodAttributes.Public, property.PropertyType, new Type[0]);
      var ilGet = getMethod.GetILGenerator();
      ilGet.Emit(OpCodes.Ldarg_0);
      ilGet.Emit(OpCodes.Ldfld, fieldBuilder);
      ilGet.Emit(OpCodes.Ret);

      propertyBuilder.SetGetMethod(getMethod);
      typeBuilder.DefineMethodOverride(getMethod, property.GetGetMethod());

      var setMethod = typeBuilder.DefineMethod("set_" + propertyBuilder.Name, MethodAttributes.Virtual | MethodAttributes.Public, typeof(void), new[] { property.PropertyType });
      setMethod.DefineParameter(0, ParameterAttributes.None, "value");
      var ilSet = setMethod.GetILGenerator();
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

    static string MakeFieldName(PropertyInfo property)
    {
      return "_" + property.Name;
    }
    
    static void ReturnIfNull(ILGenerator il, Action ifNull)
    {
      var nope = il.DefineLabel();
      il.Emit(OpCodes.Brtrue, nope);
      ifNull();
      il.MarkLabel(nope);
    }

    private static void IfFalse(ILGenerator il, Action isTrue, Action isFalse)
    {
      var yep = il.DefineLabel();
      var done = il.DefineLabel();
      il.Emit(OpCodes.Brtrue, yep);
      isTrue();
      il.Emit(OpCodes.Br, done);
      il.MarkLabel(yep);
      isFalse();
      il.MarkLabel(done);
    }

    private static void IfNull(ILGenerator il, Action isTrue, Action isFalse)
    {
      var nope = il.DefineLabel();
      var done = il.DefineLabel();
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
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
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
      var code = 0;
      for (var i = 0; i < array.Length; ++i)
      {
        var value = array.GetValue(i);
        if (value != null)
        {
          code ^= value.GetHashCode();
        }
      }
      return code;
    }

    public static string ToString(Array array)
    {
      if (array == null) return "(null)";
      var sb = new StringBuilder();
      sb.Append("[");
      for (var i = 0; i < array.Length; ++i)
      {
        if (i != 0)
        {
          sb.Append(", ");
        }
        var value = array.GetValue(i);
        if (value != null)
        {
          sb.Append(value.ToString());
        }
        else
        {
          sb.Append("(null)");
        }
      }
      sb.Append("]");
      return sb.ToString();
    }
  }

  public class Blah
  {
    public void Run()
    {
      DateTime? value = null;
      if (value != null)
      {
        Console.WriteLine(value.Value);
      }
    }
  }
}