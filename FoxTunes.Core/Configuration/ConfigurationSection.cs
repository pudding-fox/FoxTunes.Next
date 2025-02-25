using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace FoxTunes
{
    public class ConfigurationSection : INotifyPropertyChanged
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public ConfigurationSection()
        {
            this.Elements = new Dictionary<string, ConfigurationElement>(StringComparer.OrdinalIgnoreCase);
            this.Flags = ConfigurationSectionFlags.None;
        }

        public ConfigurationSection(string id, string name = null, string description = null)
            : this()
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
        }

        public string Id { get; set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IDictionary<string, ConfigurationElement> Elements { get; protected set; }

        public bool IsInitialized { get; protected set; }

        public ConfigurationSection WithElement(ConfigurationElement element)
        {
            if (this.Contains(element.Id))
            {
                this.Update(element);
            }
            else
            {
                this.Add(element);
            }
            return this;
        }

        public bool Contains(string id)
        {
            return this.GetElement(id) != null;
        }

        private ConfigurationSectionFlags _Flags { get; set; }

        public ConfigurationSectionFlags Flags
        {
            get
            {
                return this._Flags;
            }
            set
            {
                this._Flags = value;
                this.OnFlagsChanged();
            }
        }

        protected virtual void OnFlagsChanged()
        {
            if (this.FlagsChanged != null)
            {
                this.FlagsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Flags");
        }

        public event EventHandler FlagsChanged;

        public ConfigurationSection WithFlags(ConfigurationSectionFlags flags)
        {
            this.Flags = flags;
            return this;
        }

        public virtual void InitializeComponent()
        {
            foreach (var pair in this.Elements)
            {
                if (pair.Value.IsInitialized)
                {
                    continue;
                }
                pair.Value.InitializeComponent();
            }
            this.IsInitialized = true;
        }

        public void Update(ConfigurationSection section)
        {
            if (string.IsNullOrEmpty(this.Name) && !string.IsNullOrEmpty(section.Name))
            {
                this.Name = section.Name;
            }
            if (string.IsNullOrEmpty(this.Description) && !string.IsNullOrEmpty(section.Description))
            {
                this.Description = section.Description;
            }
            this.Flags |= section.Flags;
            foreach (var pair in section.Elements)
            {
                if (!this.Contains(pair.Key))
                {
                    this.Add(pair.Value);
                }
                else
                {
                    this.Update(pair.Value);
                }
            }
        }

        protected virtual void Add(ConfigurationElement element)
        {
            Logger.Write(typeof(ConfigurationSection), LogLevel.Debug, "Adding configuration element: {0} => {1}", element.Id, element.Name);
            this.Elements.Add(element.Id, element);
        }

        protected virtual void Update(ConfigurationElement element)
        {
            Logger.Write(typeof(ConfigurationSection), LogLevel.Debug, "Updating configuration element: {0} => {1}", element.Id, element.Name);
            var existing = this.GetElement(element.Id);
            existing.Update(element);
        }

        private void Hide(ConfigurationElement element)
        {
            Logger.Write(typeof(ConfigurationSection), LogLevel.Debug, "Hiding configuration element: {0} => {1}", element.Id, element.Name);
            var existing = this.GetElement(element.Id);
            existing.Hide();
        }

        public T GetElement<T>(string elementId) where T : ConfigurationElement
        {
            return this.GetElement(elementId) as T;
        }

        public virtual ConfigurationElement GetElement(string elementId)
        {
            var element = default(ConfigurationElement);
            if (this.Elements.TryGetValue(elementId, out element))
            {
                return element;
            }
            return default(ConfigurationElement);
        }

        public void Reset()
        {
            foreach (var pair in this.Elements)
            {
                pair.Value.Reset();
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged == null)
            {
                return;
            }
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    [Flags]
    public enum ConfigurationSectionFlags : byte
    {
        None = 0,
        System = 1
    }
}
