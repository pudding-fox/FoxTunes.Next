using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public interface IBassEncoderSettings : IBaseComponent
    {
        string Name { get; }

        string Extension { get; }

        IBassEncoderFormat Format { get; }

        BassEncoderSettingsFlags Flags { get; }

        void InitializeComponent(ICore core);

        long GetLength(EncoderItem encoderItem, IBassStream stream);

        int GetDepth(EncoderItem encoderItem, IBassStream stream);

        int GetRate(EncoderItem encoderItem, IBassStream stream);

        int GetChannels(EncoderItem encoderItem, IBassStream stream);
    }

    [Flags]
    public enum BassEncoderSettingsFlags : byte
    {
        None,
        Internal = 1
    }
}
