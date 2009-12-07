using System;
using System.Linq;
using NServiceBus;

namespace Machine.Mta
{
  public static class NsbMessageHelpers
  {
    public static NServiceBus.IMessage[] ToNsbMessages<T>(this T[] messages) where T : IMessage
    {
      return messages.Cast<NServiceBus.IMessage>().ToArray();
    }

    public static string ToNsbAddress(this EndpointAddress address)
    {
      return address.ToNameAndHost().ToNsbAddress();
    }

    static string ToNsbAddress(this NameAndHostAddress address)
    {
      return address.Name + "@" + address.Host;
    }
  }
}