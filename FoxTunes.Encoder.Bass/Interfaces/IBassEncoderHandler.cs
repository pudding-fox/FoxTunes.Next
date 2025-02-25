namespace FoxTunes.Interfaces
{
    public interface IBassEncoderHandler : IBassEncoderSettings
    {
        IBassEncoderWriter GetWriter(EncoderItem encoderItem, IBassStream stream);
    }
}
