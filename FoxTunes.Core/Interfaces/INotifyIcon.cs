using System;
namespace FoxTunes.Interfaces
{
    public interface INotifyIcon : IConfigurableComponent, IDisposable
    {
        IMessageSink MessageSink { get; }

        IntPtr Icon { get; set; }

        void Show();

        bool Update();

        void Hide();
    }
}
