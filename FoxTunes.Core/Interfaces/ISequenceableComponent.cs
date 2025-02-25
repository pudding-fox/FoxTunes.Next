namespace FoxTunes.Interfaces
{
    public interface ISequenceableComponent : IBaseComponent
    {
        int Sequence { get; set; }
    }
}
