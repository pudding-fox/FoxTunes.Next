using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamPipeline : IBassStreamControllable, IBaseComponent, IDisposable
    {
        IBassStreamInput Input { get; }

        IEnumerable<IBassStreamComponent> Components { get; }

        IBassStreamOutput Output { get; }

        bool IsStarting { set; }

        bool IsStopping { set; }

        int BufferLength { get; }

        void InitializeComponent(IBassStreamInput input, IEnumerable<IBassStreamComponent> components, IBassStreamOutput output);

        void Connect(BassOutputStream stream);

        void ClearBuffer();
    }
}
