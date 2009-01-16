using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Machine.Mta.InterfacesAsMessages
{
  public class FieldBackedMessageInterfaceImplementationFactory : MessageInterfaceImplementationFactory
  {
    protected override void ImplementMessage(TypeBuilder typeBuilder, Type type)
    {
    }

    protected override void ImplementProperty(TypeBuilder typeBuilder, PropertyInfo property)
    {
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

    private static string MakeFieldName(PropertyInfo property)
    {
      return "_" + property.Name;
    }
  }
}