using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Unicast.Transport;

namespace Machine.Mta
{
  public class MtaCompatibilityHeadersSerializer : IHeadersSerializer
  {
    readonly static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MtaCompatibilityHeadersSerializer));
    readonly XmlHeadersSerializer _serializer = new XmlHeadersSerializer();

    public byte[] Serialize(IList<HeaderInfo> headers)
    {
      if (CurrentSagaIds.OutgoingIds != null && CurrentSagaIds.OutgoingIds.Length > 0)
      {
        var value = String.Join(",", CurrentSagaIds.OutgoingIds.Select(x => x.ToString()).ToArray());
        var header = new HeaderInfo()
        {
          Key = "machine.sagas",
          Value = value
        };
        _log.Debug("Serializing: " + value);
        return _serializer.Serialize(headers.Union(new[] { header }).ToList());
      }
      return _serializer.Serialize(headers);
    }

    public HeaderInfo[] Deserialize(byte[] buffer)
    {
      CurrentSagaIds.UseIncoming(new Guid[0]);
      var headers = _serializer.Deserialize(buffer);
      var sagaHeader = headers.SingleOrDefault(h => h.Key == "machine.sagas");
      if (sagaHeader != null)
      {
        _log.Debug("Deserializing: " + sagaHeader.Value);
        var guids = sagaHeader.Value.Split(',').Where(x => !String.IsNullOrEmpty(x)).Select(x => new Guid(x)).ToArray();
        CurrentSagaIds.UseIncoming(guids);
      }
      return headers;
    }
  }
}
