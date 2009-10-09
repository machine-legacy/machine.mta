using System;
using System.Runtime.Serialization;

namespace Machine.Mta.InterfacesAsMessages
{
  public class MessageInterfaceSerializationBinder : SerializationBinder
  {
    public override Type BindToType(string assemblyName, string typeName)
    {
      return MessageInterfaceHelpers.FindTypeNamed(typeName, false);
    }
  }
}