using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Unicast.Transport;

namespace Machine.Mta
{
  public class MtaCompatibilityHeadersSerializer : IHeadersSerializer
  {
    readonly XmlHeadersSerializer _serializer = new XmlHeadersSerializer();

    public byte[] Serialize(IEnumerable<HeaderInfo> headers)
    {
      var cmc = CurrentMessageContext.Current;
      if (cmc != null)
      {
        var value = String.Join(",", cmc.SagaIds.Select(x => x.ToString()).ToArray());
        var header = new HeaderInfo() {
          Key = "machine.sagas",
          Value = value
        };
        return _serializer.Serialize(headers.Union(new[] { header }));
      }
      return _serializer.Serialize(headers);
    }

    public HeaderInfo[] Deserialize(byte[] buffer)
    {
      CurrentSagaIds.Use(new Guid[0]);
      var headers = _serializer.Deserialize(buffer);
      var sagaHeader = headers.SingleOrDefault(h => h.Key == "machine.sagas");
      if (sagaHeader != null)
      {
        var guids = sagaHeader.Value.Split(',').Where(x => !String.IsNullOrEmpty(x)).Select(x => new Guid(x)).ToArray();
        CurrentSagaIds.Use(guids);
      }
      return headers;
    }
  }

  public static class CurrentSagaIds
  {
    [ThreadStatic]
    public static Guid[] _sagaIds;

    public static Guid[] Ids
    {
      get { return _sagaIds; }
    }

    public static void Use(Guid[] sagaIds)
    {
      _sagaIds = sagaIds;
    }

    public static void CopyToOutgoing()
    {
    }
  }
}
