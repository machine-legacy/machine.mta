using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

using Machine.Mta.InterfacesAsMessages;

namespace Machine.Mta
{
  public class Serializers
  {
    static BinaryFormatter _binaryFormatter;

    static Serializers()
    { 
      _binaryFormatter = new BinaryFormatter();
      _binaryFormatter.Binder = new MessageInterfaceSerializationBinder();
    }

    public static BinaryFormatter Binary
    {
      get { return _binaryFormatter; }
      set { _binaryFormatter = value; }
    }
  }
}
