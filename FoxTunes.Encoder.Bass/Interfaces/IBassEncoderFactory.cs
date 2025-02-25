using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public interface IBassEncoderFactory : IStandardComponent
    {
        IBassEncoder CreateEncoder(IEnumerable<EncoderItem> encoderItems);
    }
}
