using FoxTunes.Interfaces;

namespace FoxTunes
{
    public abstract class BassEncoderHandler : BassEncoderSettings, IBassEncoderHandler
    {
        public abstract IBassEncoderWriter GetWriter(EncoderItem encoderItem, IBassStream stream);
    }
}
