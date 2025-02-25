using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamInput : IBassStreamComponent
    {
        IEnumerable<Type> SupportedProviders { get; }

        bool PreserveBuffer { get; }

        bool CheckFormat(BassOutputStream stream);

        void Connect(BassOutputStream stream);

        bool Contains(BassOutputStream stream);

        bool Add(BassOutputStream stream, Action<BassOutputStream> callBack);

        bool Remove(BassOutputStream stream, Action<BassOutputStream> callBack);

        void Reset();
    }
}
