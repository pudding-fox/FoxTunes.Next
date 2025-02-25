using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace FoxTunes
{
    public abstract class ThemeBase : StandardComponent, ITheme
    {
        private ThemeBase()
        {
            this.ResourceDictionary = new Lazy<ResourceDictionary>(this.GetResourceDictionary);
            this.ResourceDictionaries = new Dictionary<ConfigurationElement, Lazy<ResourceDictionary>>();
        }

        protected ThemeBase(string id, string name, string description, IEnumerable<IColorPalette> colorPalettes) : this()
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.ColorPalettes = colorPalettes;
        }

        public Lazy<ResourceDictionary> ResourceDictionary { get; private set; }

        public IDictionary<ConfigurationElement, Lazy<ResourceDictionary>> ResourceDictionaries { get; private set; }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public bool IsEnabled { get; private set; }

        public abstract ResourceDictionary GetResourceDictionary();

        public abstract Stream GetArtworkPlaceholder();

        public IEnumerable<IColorPalette> ColorPalettes { get; private set; }

        public virtual ThemeFlags Flags
        {
            get
            {
                return ThemeFlags.None;
            }
        }

        public IConfiguration Configuration { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            base.InitializeComponent(core);
        }

        public void Enable()
        {
            try
            {
                Application.Current.Resources.MergedDictionaries.Add(this.ResourceDictionary.Value);
                foreach (var pair in this.ResourceDictionaries)
                {
                    var element = pair.Key as BooleanConfigurationElement;
                    if (element == null)
                    {
                        continue;
                    }
                    if (element.Value)
                    {
                        Application.Current.Resources.MergedDictionaries.Add(pair.Value.Value);
                    }
                }
            }
            finally
            {
                this.IsEnabled = true;
            }
        }

        public void Disable()
        {
            try
            {
                if (this.ResourceDictionary.IsValueCreated)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(this.ResourceDictionary.Value);
                }
                foreach (var pair in this.ResourceDictionaries)
                {
                    if (pair.Value.IsValueCreated)
                    {
                        Application.Current.Resources.MergedDictionaries.Remove(pair.Value.Value);
                    }
                }
            }
            finally
            {
                this.IsEnabled = false;
            }
        }

        public class ColorPalette : BaseComponent, IColorPalette
        {
            public ColorPalette(string id, ColorPaletteRole role, string name, string description, string value, ColorPaletteFlags flags = ColorPaletteFlags.None)
            {
                this.Id = id;
                this.Role = role;
                this.Name = name;
                this.Description = description;
                this.Value = value;
                this.Flags = flags;
            }

            public string Id { get; private set; }

            public ColorPaletteRole Role { get; private set; }

            public string Name { get; private set; }

            public string Description { get; private set; }

            public string Value { get; private set; }

            public ColorPaletteFlags Flags { get; private set; }
        }
    }
}
