using NServiceBus;

namespace Machine.Mta
{
  public class NsbBus
  {
    readonly IStartableBus _startableBus;

    public IStartableBus StartableBus
    {
      get { return _startableBus; }
    }

    public IBus Bus
    {
      get { return _startableBus.Start(); }
    }

    public NsbBus(IStartableBus startableBus)
    {
      _startableBus = startableBus;
    }

    public void Start()
    {
      _startableBus.Start();
    }
  }
}