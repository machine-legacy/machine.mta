using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace Machine.Mta
{
  public class Serializers
  {
    static BinaryFormatter _binaryFormatter;

    static Serializers()
    { 
      _binaryFormatter = new BinaryFormatter();
    }

    public static BinaryFormatter Binary
    {
      get { return _binaryFormatter; }
      set { _binaryFormatter = value; }
    }
  }
}
