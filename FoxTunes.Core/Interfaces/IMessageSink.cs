using System;

namespace FoxTunes.Interfaces
{
    public interface IMessageSink : IBaseComponent, IDisposable
    {
        IntPtr Handle { get; }

        event EventHandler MouseLeftButtonDown;

        event EventHandler MouseLeftButtonUp;

        event EventHandler MouseRightButtonDown;

        event EventHandler MouseRightButtonUp;

        event EventHandler MouseMove;

        event EventHandler MouseDoubleClick;

        event EventHandler TaskBarCreated;
    }
}
