using System;

namespace FoxTunes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class UIComponentAttribute : Attribute
    {
        public UIComponentAttribute(string id, int children = NO_CHILDREN, UIComponentRole role = UIComponentRole.None)
        {
            this.Id = id;
            this.Children = children;
            this.Role = role;
        }

        public string Id { get; private set; }

        public int Children { get; private set; }

        public UIComponentRole Role { get; private set; }

        public const int NO_CHILDREN = 0;

        public const int ONE_CHILD = 1;

        public const int UNLIMITED_CHILDREN = -1; 
    }

    public enum UIComponentRole : byte
    {
        None,
        Container,
        Visualization,
        Playback,
        Info,
        System,
        Launcher,
        Playlist,
        Library,
        DSP
    }
}
