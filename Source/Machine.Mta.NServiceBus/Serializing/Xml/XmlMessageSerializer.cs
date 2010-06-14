using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

using Common.Logging;
using NServiceBus.Encryption;
using NServiceBus.MessageInterfaces;
using NServiceBus.Serialization;

namespace Machine.Mta.Serializing.Xml
{
  public class XmlMessageSerializer : IMessageSerializer
  {
    IMessageMapper _messageMapper;

    public IMessageMapper MessageMapper
    {
      get { return _messageMapper; }
      set
      {
        _messageMapper = value;
        if (_messageTypes != null)
        {
          _messageMapper.Initialize(_messageTypes);
        }
      }
    }

    private string _nameSpace = "http://tempuri.net";

    public string Namespace
    {
      get { return _nameSpace; }
      set { _nameSpace = value; }
    }

    private List<Type> _additionalTypes;

    public List<Type> AdditionalTypes
    {
      get { return _additionalTypes; }
      set { _additionalTypes = value; }
    }

    private List<Type> _messageTypes;

    public List<Type> MessageTypes
    {
      get { return _messageTypes; }
      set
      {
        _messageTypes = value;
        if (!_messageTypes.Contains(typeof(EncryptedValue)))
        {
          _messageTypes.Add(typeof(EncryptedValue));
        }
        if (this.MessageMapper != null)
        {
          this.MessageMapper.Initialize(_messageTypes.ToArray());
        }
        foreach (Type t in _messageTypes)
        {
          this.InitType(t);
        }
      }
    }

    public void InitType(Type type)
    {
      if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime))
        return;

      if (typeof(IEnumerable).IsAssignableFrom(type))
      {
        if (type.IsArray)
          _typesToCreateForArrays[type] = typeof(List<>).MakeGenericType(type.GetElementType());

        foreach (Type g in type.GetGenericArguments())
          InitType(g);

        //Handle dictionaries - initalize relevant KeyValuePair<T,K> types.
        foreach (Type interfaceType in type.GetInterfaces())
        {
          Type[] arr = interfaceType.GetGenericArguments();
          if (arr.Length == 1)
            if (typeof(IEnumerable<>).MakeGenericType(arr[0]).IsAssignableFrom(type))
              InitType(arr[0]);
        }

        return;
      }

      //already in the process of initializing this type (prevents infinite recursion).
      if (_typesBeingInitialized.Contains(type))
        return;

      _typesBeingInitialized.Add(type);

      var props = GetAllPropertiesForType(type);
      _typeToProperties[type] = props;
      var fields = GetAllFieldsForType(type);
      _typeToFields[type] = fields;

      foreach (PropertyInfo prop in props)
        InitType(prop.PropertyType);

      foreach (FieldInfo field in fields)
        InitType(field.FieldType);
    }

    static IEnumerable<PropertyInfo> GetAllPropertiesForType(Type type)
    {
      List<PropertyInfo> result = new List<PropertyInfo>(type.GetProperties());

      if (type.IsInterface)
        foreach (Type interfaceType in type.GetInterfaces())
          result.AddRange(GetAllPropertiesForType(interfaceType));

      return result;
    }

    static IEnumerable<FieldInfo> GetAllFieldsForType(Type type)
    {
      return type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
    }

    public NServiceBus.IMessage[] Deserialize(Stream stream)
    {
      _prefixesToNamespaces = new Dictionary<string, string>();
      _messageBaseTypes = new List<Type>();
      List<NServiceBus.IMessage> result = new List<NServiceBus.IMessage>();

      XmlDocument doc = new XmlDocument();
      doc.Load(stream);

      foreach (XmlAttribute attr in doc.DocumentElement.Attributes)
      {
        if (attr.Name == "xmlns")
          _defaultNameSpace = attr.Value.Substring(attr.Value.LastIndexOf("/") + 1);
        else
        {
          if (attr.Name.Contains("xmlns:"))
          {
            int colonIndex = attr.Name.LastIndexOf(":");
            string prefix = attr.Name.Substring(colonIndex + 1);

            if (prefix.Contains(BASETYPE))
            {
              Type baseType = MessageMapper.GetMappedTypeFor(attr.Value);
              if (baseType != null)
                _messageBaseTypes.Add(baseType);
            }
            else
              _prefixesToNamespaces[prefix] = attr.Value;
          }
        }
      }

      if (doc.DocumentElement.Name.ToLower() != "messages")
      {
        object m = Process(doc.DocumentElement, null);

        if (m == null)
          throw new SerializationException("Could not deserialize message.");

        result.Add(m as NServiceBus.IMessage);
      }
      else
      {
        foreach (XmlNode node in doc.DocumentElement.ChildNodes)
        {
          object m = Process(node, null);

          result.Add(m as NServiceBus.IMessage);
        }
      }

      _defaultNameSpace = null;

      return result.ToArray();
    }

    object Process(XmlNode node, object parent)
    {
      string name = node.Name;
      string typeName = _defaultNameSpace + "." + name;

      if (name.Contains(":"))
      {
        int colonIndex = node.Name.IndexOf(":");
        name = name.Substring(colonIndex + 1);
        string prefix = node.Name.Substring(0, colonIndex);
        string nameSpace = _prefixesToNamespaces[prefix];

        typeName = nameSpace.Substring(nameSpace.LastIndexOf("/") + 1) + "." + name;
      }

      if (parent != null)
      {
        if (parent is IEnumerable)
        {
          if (parent.GetType().IsArray)
            return GetObjectOfTypeFromNode(parent.GetType().GetElementType(), node);

          var args = parent.GetType().GetGenericArguments();
          if (args.Length == 1)
            return GetObjectOfTypeFromNode(args[0], node);
        }

        PropertyInfo prop = parent.GetType().GetProperty(name);
        if (prop != null)
          return GetObjectOfTypeFromNode(prop.PropertyType, node);
      }

      Type t = MessageMapper.GetMappedTypeFor(typeName);
      if (t == null)
      {
        _logger.Debug("Could not load " + typeName + ". Trying base types...");
        foreach (Type baseType in _messageBaseTypes)
          try
          {
            _logger.Debug("Trying to deserialize message to " + baseType.FullName);
            return GetObjectOfTypeFromNode(baseType, node);
          }
          catch { } // intentionally swallow exception

        throw new TypeLoadException("Could not handle type '" + typeName + "'.");
      }

      return GetObjectOfTypeFromNode(t, node);
    }

    class DeserializedProperty
    {
      public object Value;
      public Action<object> Set;
    }

    object GetObjectOfTypeFromNode(Type type, XmlNode node)
    {
      if (type.IsSimpleType())
        return GetPropertyValue(type, node);

      Dictionary<MemberInfo, DeserializedProperty> properties = new Dictionary<MemberInfo, DeserializedProperty>();

      foreach (XmlNode n in node.ChildNodes)
      {
        PropertyInfo prop = GetProperty(type, n.Name);
        if (prop != null)
        {
          object val = GetPropertyValue(prop.PropertyType, n);
          if (val != null)
            properties[prop] = new DeserializedProperty() { Value = val, Set = (o) => prop.SetValue(o, val, null) };
        }

        FieldInfo field = GetField(type, n.Name);
        if (field != null)
        {
          object val = GetPropertyValue(field.FieldType, n);
          if (val != null)
            properties[field] = new DeserializedProperty() { Value = val, Set = (o) => field.SetValue(o, val) };
        }
      }

      if (type.IsInterface || type.IsAbstract || ExtensionMethods.HasDefaultConstructor(type))
      {
        object result = MessageMapper.CreateInstance(type);
        foreach (var entry in properties)
        {
          entry.Value.Set(result);
        }
        return result;
      }
      return CreateUsingNonDefaultConstructor(type, properties);
    }

    static object CreateUsingNonDefaultConstructor(Type type, IDictionary<MemberInfo, DeserializedProperty> properties)
    {
      var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).SingleOrDefault();
      if (ctor == null) throw new ArgumentException("No valid constructor on: " + type);
      var source = ctor.GetParameters().ToDictionary<ParameterInfo, ParameterInfo, object>(p => p, p => null);
      foreach (var entry in properties)
      {
        var key = source.ForgivingCaseSensitiveFind(kv => kv.Key.Name, entry.Key.Name).Key;
        if (key != null)
        {
          source[key] = entry.Value.Value;
        }
      }
      return Activator.CreateInstance(type, source.Values.ToArray());
    }

    static PropertyInfo GetProperty(Type t, string name)
    {
      IEnumerable<PropertyInfo> props;
      _typeToProperties.TryGetValue(t, out props);

      if (props == null)
        return null;

      foreach (PropertyInfo prop in props)
        if (prop.Name == name)
          return prop;

      return null;
    }

    static FieldInfo GetField(Type t, string name)
    {
      IEnumerable<FieldInfo> fields;
      _typeToFields.TryGetValue(t, out fields);

      if (fields == null)
        return null;

      foreach (FieldInfo f in fields)
        if (f.Name == name)
          return f;

      return null;
    }

    object GetPropertyValue(Type type, XmlNode n)
    {
      var xsiType = n.Attributes["type", "http://www.w3.org/2001/XMLSchema-instance"];
      if (xsiType != null)
      {
        type = this.MessageMapper.GetMappedTypeFor(xsiType.Value);
      }
      if (n.ChildNodes.Count == 1 && n.ChildNodes[0] is XmlText)
      {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
          type = type.GetGenericArguments().First();

        if (type == typeof(string))
          return n.ChildNodes[0].InnerText;

        if (type.IsPrimitive || type == typeof(decimal))
          return Convert.ChangeType(n.ChildNodes[0].InnerText, type);

        if (type == typeof(Guid))
          return new Guid(n.ChildNodes[0].InnerText);

        if (type == typeof(DateTime))
          return XmlConvert.ToDateTime(n.ChildNodes[0].InnerText, XmlDateTimeSerializationMode.Utc);

        if (type == typeof(TimeSpan))
          return XmlConvert.ToTimeSpan(n.ChildNodes[0].InnerText);

        if (type == typeof(DateTimeOffset))
          return DateTimeOffset.Parse(n.ChildNodes[0].InnerText, null, System.Globalization.DateTimeStyles.RoundtripKind);

        if (type == typeof(Uri))
          return new Uri(n.ChildNodes[0].InnerText);

        if (type.IsEnum)
          return Enum.Parse(type, n.ChildNodes[0].InnerText);
      }

      //Handle dictionaries
      Type[] arr = type.GetGenericArguments();
      if (arr.Length == 2)
      {
        if (typeof(IDictionary<,>).MakeGenericType(arr).IsAssignableFrom(type))
        {
          IDictionary result = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(arr)) as IDictionary;

          foreach (XmlNode xn in n.ChildNodes) // go over KeyValuePairs
          {
            object key = null;
            object value = null;

            foreach (XmlNode node in xn.ChildNodes)
            {
              if (node.Name == "Key")
                key = GetObjectOfTypeFromNode(arr[0], node);
              if (node.Name == "Value")
                value = GetObjectOfTypeFromNode(arr[1], node);
            }

            result[key] = value;
          }

          return result;
        }
      }

      if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
      {
        bool isArray = type.IsArray;

        Type typeToCreate = type;
        if (isArray)
          typeToCreate = _typesToCreateForArrays[type];

        IList list = Activator.CreateInstance(typeToCreate) as IList;

        foreach (XmlNode xn in n.ChildNodes)
        {
          object m = Process(xn, list);

          if (list != null)
            list.Add(m);
        }

        if (isArray)
          return typeToCreate.GetMethod("ToArray").Invoke(list, null);
        else
          return list;
      }

      if (n.ChildNodes.Count == 0 && xsiType == null)
      {
        if (type == typeof(string))
          return string.Empty;
        else
          return null;
      }


      return GetObjectOfTypeFromNode(type, n);
    }

    public void Serialize(NServiceBus.IMessage[] messages, Stream stream)
    {
      _namespacesToPrefix = new Dictionary<string, string>();

      StringBuilder builder = new StringBuilder();

      List<string> namespaces = GetNamespaces(messages, MessageMapper);
      List<string> baseTypes = GetBaseTypes(messages, MessageMapper);

      builder.AppendLine("<?xml version=\"1.0\" ?>");

      builder.Append("<Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"");

      for (var i = 0; i < namespaces.Count; i++)
      {
        string prefix = "q" + i;
        if (i == 0)
          prefix = "";

        builder.AppendFormat(" xmlns{0}=\"{1}/{2}\"", (prefix != "" ? ":" + prefix : prefix), _nameSpace, namespaces[i]);
        _namespacesToPrefix[namespaces[i]] = prefix;
      }

      for (var i = 0; i < baseTypes.Count; i++)
      {
        string prefix = BASETYPE;
        if (i != 0)
          prefix += i;

        builder.AppendFormat(" xmlns:{0}=\"{1}\"", prefix, baseTypes[i]);
      }

      builder.Append(">\n");

      foreach (var m in messages)
      {
        Type t = MessageMapper.GetMappedTypeFor(m.GetType());

        WriteObject(t.Name, t, t, m, builder);
      }

      builder.AppendLine("</Messages>");

      byte[] buffer = UnicodeEncoding.UTF8.GetBytes(builder.ToString());
      stream.Write(buffer, 0, buffer.Length);
    }

    void Write(StringBuilder builder, Type t, object obj)
    {
      if (obj == null)
        return;

      if (!_typeToProperties.ContainsKey(t))
      {
        throw new InvalidOperationException("No registered message type: " + t);
      }

      foreach (var prop in _typeToProperties[t])
      {
        WriteEntry(prop.Name, prop.PropertyType, prop.GetValue(obj, null), builder);
      }

      foreach (var field in _typeToFields[t])
      {
        WriteEntry(field.Name, field.FieldType, field.GetValue(obj), builder);
      }
    }

    void WriteObject(string name, Type type, Type typeOnWire, object value, StringBuilder builder)
    {
      string element = name;
      string prefix;
      _namespacesToPrefix.TryGetValue(type.Namespace, out prefix);

      if (!string.IsNullOrEmpty(prefix))
        element = prefix + ":" + name;

      if (type != typeOnWire)
      {
        builder.AppendFormat("<{0} xsi:type=\"{1}\">\n", element, typeOnWire.FullName);
        Write(builder, typeOnWire, value);
      }
      else
      {
        builder.AppendFormat("<{0}>\n", element);
        Write(builder, type, value);
      }

      builder.AppendFormat("</{0}>\n", element);
    }

    void WriteEntry(string name, Type type, object value, StringBuilder builder)
    {
      if (value == null)
        return;

      if (CanFormatAsString(type))
      {
        builder.AppendFormat("<{0}>{1}</{0}>\n", name, FormatAsString(value));
        return;
      }

      if (typeof(IEnumerable).IsAssignableFrom(type))
      {
        builder.AppendFormat("<{0}>\n", name);

        Type baseType = typeof(object);

        //Get generic type from list: T for List<T>, KeyValuePair<T,K> for IDictionary<T,K>
        foreach (var interfaceType in type.GetInterfaces())
        {
          Type[] arr = interfaceType.GetGenericArguments();
          if (arr.Length == 1)
            if (typeof(IEnumerable<>).MakeGenericType(arr[0]).IsAssignableFrom(type))
            {
              baseType = arr[0];
              break;
            }
        }

        foreach (var obj in ((IEnumerable)value))
          if (obj.GetType().IsSimpleType())
            WriteEntry(obj.GetType().Name, obj.GetType(), obj, builder);
          else
            WriteObject(baseType.SerializationFriendlyName(), baseType, baseType, obj, builder);

        builder.AppendFormat("</{0}>\n", name);
        return;
      }

      var mappedType = MessageMapper.GetMappedTypeFor(value.GetType());
      WriteObject(name, type, mappedType, value, builder);
    }

    static string FormatAsString(object value)
    {
      if (value == null)
        return string.Empty;
      if (value is bool)
        return value.ToString().ToLower();
      if (value is string)
        return System.Security.SecurityElement.Escape(value as string);
      if (value is DateTime)
        return ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
      if (value is TimeSpan)
      {
        TimeSpan ts = (TimeSpan)value;
        return string.Format("{0}P0Y0M{1}DT{2}H{3}M{4}.{5:000}S", (ts.TotalSeconds < 0 ? "-" : ""), Math.Abs(ts.Days), Math.Abs(ts.Hours), Math.Abs(ts.Minutes), Math.Abs(ts.Seconds), Math.Abs(ts.Milliseconds));
      }
      if (value is DateTimeOffset)
        return ((DateTimeOffset)value).ToString("o");
      if (value is Guid)
        return ((Guid)value).ToString();
      if (value is Uri)
        return ((Uri)value).ToString();

      return value.ToString();
    }

    static bool CanFormatAsString(Type type)
    {
      return type.IsValueType || type == typeof(Uri) || type == typeof(string);
    }

    static List<string> GetNamespaces(IEnumerable<NServiceBus.IMessage> messages, IMessageMapper mapper)
    {
      List<string> result = new List<string>();

      foreach (var m in messages)
      {
        string ns = mapper.GetMappedTypeFor(m.GetType()).Namespace;
        if (!result.Contains(ns))
          result.Add(ns);
      }

      return result;
    }

    static List<string> GetBaseTypes(IEnumerable<NServiceBus.IMessage> messages, IMessageMapper mapper)
    {
      List<string> result = new List<string>();

      foreach (var m in messages)
      {
        Type t = mapper.GetMappedTypeFor(m.GetType());

        Type baseType = t.BaseType;
        while (baseType != typeof(object) && baseType != null)
        {
          if (typeof(NServiceBus.IMessage).IsAssignableFrom(baseType))
            if (!result.Contains(baseType.FullName))
              result.Add(baseType.FullName);

          baseType = baseType.BaseType;
        }

        foreach (Type i in t.GetInterfaces())
          if (i != typeof(NServiceBus.IMessage) && typeof(NServiceBus.IMessage).IsAssignableFrom(i))
            if (!result.Contains(i.FullName))
              result.Add(i.FullName);
      }

      return result;
    }

    static readonly string XMLPREFIX = "d1p1";
    static readonly string XMLTYPE = XMLPREFIX + ":type";
    static readonly string BASETYPE = "baseType";

    static readonly Dictionary<Type, IEnumerable<PropertyInfo>> _typeToProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
    static readonly Dictionary<Type, IEnumerable<FieldInfo>> _typeToFields = new Dictionary<Type, IEnumerable<FieldInfo>>();
    static readonly Dictionary<Type, Type> _typesToCreateForArrays = new Dictionary<Type, Type>();
    static readonly List<Type> _typesBeingInitialized = new List<Type>();

    [ThreadStatic]
    static string _defaultNameSpace;
    [ThreadStatic]
    static IDictionary<string, string> _namespacesToPrefix;
    [ThreadStatic]
    static IDictionary<string, string> _prefixesToNamespaces;
    [ThreadStatic]
    static List<Type> _messageBaseTypes;
    static readonly ILog _logger = LogManager.GetLogger("NServiceBus.Serializers.XML");
  }

  public static class ExtensionMethods
  {
    public static bool HasDefaultConstructor(Type type)
    {
      return (type.IsValueType || (type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null) != null));
    }

    public static bool IsSimpleType(this Type type)
    {
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        return true;
      return (type == typeof(string) ||
              type.IsPrimitive ||
              type == typeof(decimal) ||
              type == typeof(Guid) ||
              type == typeof(DateTime) ||
              type == typeof(TimeSpan) ||
              type == typeof(Uri) ||
              type.IsEnum);
    }

    public static string SerializationFriendlyName(this Type type)
    {
      if (_typeToNameLookup.ContainsKey(type))
        return _typeToNameLookup[type];

      var args = type.GetGenericArguments();
      if (args != null)
      {
        int index = type.Name.IndexOf('`');
        if (index >= 0)
        {
          string result = type.Name.Substring(0, index) + "Of";
          for (int i = 0; i < args.Length; i++)
          {
            result += args[i].Name;
            if (i != args.Length - 1)
              result += "And";
          }

          _typeToNameLookup[type] = result;
          return result;
        }
      }

      _typeToNameLookup[type] = type.Name;
      return type.Name;
    }

    public static TSource ForgivingCaseSensitiveFind<TSource>(this IEnumerable<TSource> source, Func<TSource, string> valueSelector, string testValue)
    {
      if (source == null) throw new ArgumentNullException("source");
      if (valueSelector == null) throw new ArgumentNullException("valueSelector");

      var caseInsensitiveResults = source.Where(s => String.Compare(valueSelector(s), testValue, StringComparison.OrdinalIgnoreCase) == 0);
      if (caseInsensitiveResults.Count() <= 1)
      {
        return caseInsensitiveResults.SingleOrDefault();
      }
      else
      {
        // multiple results returned. now filter using case sensitivity
        var caseSensitiveResults = source.Where(s => String.Compare(valueSelector(s), testValue, StringComparison.Ordinal) == 0);
        return caseSensitiveResults.SingleOrDefault();
      }
    }

    private static IDictionary<Type, string> _typeToNameLookup = new Dictionary<Type, string>();
  }
}
