using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IOutputDeviceSelector : IStandardComponent
    {
        string Name { get; }

        bool IsActive { get; set; }

        event EventHandler IsActiveChanged;

        IEnumerable<OutputDevice> Devices { get; }

        event EventHandler DevicesChanged;

        OutputDevice Device { get; set; }

        event EventHandler DeviceChanged;

        void Refresh();

        Task ShowSettings();
    }

    public class OutputDevice : BaseComponent, IEquatable<OutputDevice>
    {
        public OutputDevice(IOutputDeviceSelector selector, string id, string name)
        {
            this.Selector = selector;
            this.Id = id;
            this.Name = name;
        }

        public IOutputDeviceSelector Selector { get; private set; }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public bool Equals(OutputDevice other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (!string.Equals(this.Selector.Name, other.Selector.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.Equals(this.Id, other.Id, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as OutputDevice);
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            if (!string.IsNullOrEmpty(this.Id))
            {
                hashCode += this.Id.GetHashCode();
            }
            return hashCode;
        }

        public static bool operator ==(OutputDevice a, OutputDevice b)
        {
            if ((object)a == null && (object)b == null)
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            if (object.ReferenceEquals((object)a, (object)b))
            {
                return true;
            }
            return a.Equals(b);
        }

        public static bool operator !=(OutputDevice a, OutputDevice b)
        {
            return !(a == b);
        }
    }
}
