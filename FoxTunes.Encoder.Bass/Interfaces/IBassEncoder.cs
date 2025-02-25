using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FoxTunes
{
    public interface IBassEncoder : IBaseComponent, IDisposable
    {
        Process Process { get; }

        IEnumerable<EncoderItem> EncoderItems { get; }

        void Encode();

        void Update();

        void Cancel();
    }
}
