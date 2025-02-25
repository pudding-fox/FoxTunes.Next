using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class InvocationComponent : BaseComponent, IInvocationComponent
    {
        public const string CATEGORY_GLOBAL = "75AF5307-7530-471E-8CE0-503D9AC4E430";

        public const string CATEGORY_PLAYLIST = "42BC8F63-202C-4F77-8383-04A54FFFDCD5";

        public const string CATEGORY_PLAYLIST_HEADER = "A78C8509-7862-468B-B56A-C03D68E5A5E2";

        public const string CATEGORY_PLAYLISTS = "629D58AA-351C-4380-80EA-E95A7C29DD3B";

        public const string CATEGORY_LIBRARY = "CE91AB2A-3442-4019-B585-C4DCFEF1DB65";

        public const string CATEGORY_PLAYBACK = "8302CD2E-B798-4765-81DD-A9E6DA86820A";

        public const string CATEGORY_MINI_PLAYER = "5C3822F2-6843-40B2-9A38-28EA191CC3AF";

        public const string CATEGORY_NOTIFY_ICON = "F9E060E6-D791-4E9A-A109-FD145EAECBA2";

        public const string CATEGORY_SETTINGS = "F51F1E76-255F-45A0-A053-9D57EDECE326";

        public const string CATEGORY_STREAM_POSITION = "5E4B02B6-9C09-4BAA-8936-F14D43CB0885";

        public const string CATEGORY_EQUALIZER = "4FF5C1F1-2F36-4F3B-ADE7-73A1A7B835E4";

        public const string CATEGORY_REPORT = "CD7A02FD-944C-4C8A-933F-3BB7AAB5CA49";

        public const byte ATTRIBUTE_NONE = 0;

        public const byte ATTRIBUTE_SEPARATOR = 1;

        public const byte ATTRIBUTE_SELECTED = 2;

        public const byte ATTRIBUTE_SYSTEM = 4;

        public InvocationComponent(string category, string id, string name = null, string description = null, string path = null, byte attributes = 0)
        {
            this.Category = category;
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.Path = path;
            this.Attributes = attributes;
        }

        public string Category { get; }

        public string Id { get; }

        public string Name { get; }

        public string Description { get; }

        public string Path { get; set; }

        public object Source { get; set; }

        private byte _Attributes { get; set; }

        public byte Attributes
        {
            get
            {
                return this._Attributes;
            }
            set
            {
                this._Attributes = value;
                this.OnAttributesChanged();
            }
        }

        protected virtual void OnAttributesChanged()
        {
            if (this.AttributesChanged != null)
            {
                this.AttributesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Attributes");
        }

        public event EventHandler AttributesChanged;
    }

    public static partial class Extensions
    {
        public static bool TryGetInvocation(this IInvocableComponent component, string id, out IInvocationComponent invocation)
        {
            if (component == null)
            {
                invocation = null;
                return false;
            }
            invocation = component.Invocations.FirstOrDefault(_invocation => string.Equals(_invocation.Id, id, StringComparison.OrdinalIgnoreCase));
            return invocation != null;
        }

        public static async Task<bool> TryInvoke(this IInvocableComponent component, string id, object source = null)
        {
            var invocation = default(IInvocationComponent);
            if (!component.TryGetInvocation(id, out invocation))
            {
                return false;
            }
            if (source != null)
            {
                invocation.Source = source;
            }
            await component.InvokeAsync(invocation);
            return true;
        }
    }
}
