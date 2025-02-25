using System;

namespace FoxTunes.Interfaces
{
    public interface IExpandable
    {
        bool IsExpanded { get; set; }

        event EventHandler IsExpandedChanged;
    }
}
