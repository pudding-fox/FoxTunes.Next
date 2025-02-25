using System;

namespace FoxTunes.Interfaces
{
    public interface ISelectable
    {
        bool IsSelected { get; set; }

        event EventHandler IsSelectedChanged;
    }
}
