namespace FoxTunes.Interfaces
{
    public interface ISignal
    {
        object Source { get; }

        string Name { get; }

        SignalState State { get; }
    }

    public class SignalState
    {
        public static readonly SignalState None = new SignalState();
    }
}
