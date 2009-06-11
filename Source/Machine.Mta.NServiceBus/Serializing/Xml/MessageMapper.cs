using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NServiceBus.MessageInterfaces;

namespace Machine.Mta.Serializing.Xml
{
  public class MessageMapper : IMessageMapper
  {
    public void Initialize(params Type[] types)
    {
      if (types == null || types.Length == 0)
        return;

      string name = types[0].Namespace + SUFFIX;

      AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
          new AssemblyName(name),
          AssemblyBuilderAccess.Run
          );

      ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(name);

      foreach (Type t in types)
        InitType(t, moduleBuilder);
    }

    public void InitType(Type t, ModuleBuilder moduleBuilder)
    {
      if (t == null)
        return;

      try
      {
        if (t.IsPrimitive || t == typeof(string) || t == typeof(Guid) || t == typeof(DateTime) || t == typeof(TimeSpan) || t.IsEnum || t == typeof(Uri))
          return;

        if (typeof(IEnumerable).IsAssignableFrom(t))
        {
          foreach (Type g in t.GetGenericArguments())
            InitType(g, moduleBuilder);

          InitType(t.GetElementType(), moduleBuilder);

          return;
        }

        //already handled this type, prevent infinite recursion
        if (_nameToType.ContainsKey(t.FullName))
          return;

        if (t.IsInterface)
        {
          Type mapped = CreateTypeFrom(t, moduleBuilder);
          _interfaceToConcreteTypeMapping[t] = mapped;
          _concreteToInterfaceTypeMapping[mapped] = t;
          _typeToConstructor[mapped] = mapped.GetConstructor(Type.EmptyTypes);
        }
        else
          _typeToConstructor[t] = t.GetConstructor(Type.EmptyTypes);

        _nameToType[t.FullName] = t;

        foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
          InitType(field.FieldType, moduleBuilder);

        foreach (PropertyInfo prop in t.GetProperties())
          InitType(prop.PropertyType, moduleBuilder);
      }
      catch (Exception error)
      {
        throw new InvalidOperationException("Unable to initialize type: " + t.FullName, error);
      }
    }

    public string GetNewTypeName(Type t)
    {
      return t.Namespace + SUFFIX + "." + t.Name;
    }

    public Type CreateTypeFrom(Type t, ModuleBuilder moduleBuilder)
    {
      TypeBuilder typeBuilder = moduleBuilder.DefineType(
          GetNewTypeName(t),
          TypeAttributes.Serializable | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
          typeof(object)
          );

      typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

      foreach (PropertyInfo prop in GetAllProperties(t))
      {
        Type propertyType = prop.PropertyType;

        FieldBuilder fieldBuilder = typeBuilder.DefineField(
            "field_" + prop.Name,
            propertyType,
            FieldAttributes.Private);

        PropertyBuilder propBuilder = typeBuilder.DefineProperty(
            prop.Name,
            prop.Attributes | PropertyAttributes.HasDefault,
            propertyType,
            null);

        MethodBuilder getMethodBuilder = typeBuilder.DefineMethod(
            "get_" + prop.Name,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask,
            propertyType,
            Type.EmptyTypes);

        ILGenerator getIL = getMethodBuilder.GetILGenerator();
        // For an instance property, argument zero is the instance. Load the 
        // instance, then load the private field and return, leaving the
        // field value on the stack.
        getIL.Emit(OpCodes.Ldarg_0);
        getIL.Emit(OpCodes.Ldfld, fieldBuilder);
        getIL.Emit(OpCodes.Ret);

        // Define the "set" accessor method for Number, which has no return
        // type and takes one argument of type int (Int32).
        MethodBuilder setMethodBuilder = typeBuilder.DefineMethod(
            "set_" + prop.Name,
            getMethodBuilder.Attributes,
            null,
            new Type[] { propertyType });

        ILGenerator setIL = setMethodBuilder.GetILGenerator();
        // Load the instance and then the numeric argument, then store the
        // argument in the field.
        setIL.Emit(OpCodes.Ldarg_0);
        setIL.Emit(OpCodes.Ldarg_1);
        setIL.Emit(OpCodes.Stfld, fieldBuilder);
        setIL.Emit(OpCodes.Ret);

        // Last, map the "get" and "set" accessor methods to the 
        // PropertyBuilder. The property is now complete. 
        propBuilder.SetGetMethod(getMethodBuilder);
        propBuilder.SetSetMethod(setMethodBuilder);
      }

      typeBuilder.AddInterfaceImplementation(t);

      return typeBuilder.CreateType();
    }

    private IEnumerable<PropertyInfo> GetAllProperties(Type t)
    {
      List<PropertyInfo> result = new List<PropertyInfo>(t.GetProperties());
      foreach (Type interfaceType in t.GetInterfaces())
        result.AddRange(GetAllProperties(interfaceType));

      return result;
    }

    public Type GetMappedTypeFor(Type t)
    {
      if (t.IsClass)
      {
        Type result;
        _concreteToInterfaceTypeMapping.TryGetValue(t, out result);
        if (result != null)
          return result;

        return t;
      }

      Type toReturn = null;
      _interfaceToConcreteTypeMapping.TryGetValue(t, out toReturn);

      return toReturn;
    }

    public Type GetMappedTypeFor(string typeName)
    {
      if (_nameToType.ContainsKey(typeName))
        return _nameToType[typeName];

      return Type.GetType(typeName);
    }

    public T CreateInstance<T>(Action<T> action) where T : NServiceBus.IMessage
    {
      T result = CreateInstance<T>();
      action(result);

      return result;
    }

    public T CreateInstance<T>() where T : NServiceBus.IMessage
    {
      return (T)CreateInstance(typeof(T));
    }

    public object CreateInstance(Type t)
    {
      Type mapped = t;
      if (t.IsInterface || t.IsAbstract)
      {
        mapped = GetMappedTypeFor(t);
        if (mapped == null)
          throw new ArgumentException("Could not find a concrete type mapped to " + t.FullName);
      }

      ConstructorInfo constructor = null;
      _typeToConstructor.TryGetValue(mapped, out constructor);
      if (constructor != null)
        return constructor.Invoke(null);

      return Activator.CreateInstance(mapped);
    }

    private static readonly string SUFFIX = ".__Impl";
    private static readonly Dictionary<Type, Type> _interfaceToConcreteTypeMapping = new Dictionary<Type, Type>();
    private static readonly Dictionary<Type, Type> _concreteToInterfaceTypeMapping = new Dictionary<Type, Type>();
    private static readonly Dictionary<string, Type> _nameToType = new Dictionary<string, Type>();
    private static readonly Dictionary<Type, ConstructorInfo> _typeToConstructor = new Dictionary<Type, ConstructorInfo>();
  }
}
