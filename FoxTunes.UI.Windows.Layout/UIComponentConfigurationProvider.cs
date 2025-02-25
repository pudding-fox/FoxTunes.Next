using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class UIComponentConfigurationProvider : BaseComponent, IConfiguration, IDisposable
    {
        public const string PREFIX = "Configuration_";

        public UIComponentConfigurationProvider(UIComponentConfiguration component)
        {
            this.Component = component;
            this.Sections = new Dictionary<string, ConfigurationSection>(StringComparer.OrdinalIgnoreCase);
            this.Elements = new Dictionary<string, ConfigurationElement>(StringComparer.OrdinalIgnoreCase);
        }

        public UIComponentConfiguration Component { get; private set; }

        public IEnumerable<string> AvailableProfiles
        {
            get
            {
                return Enumerable.Empty<string>();
            }
        }

        public string Profile
        {
            get
            {
                return string.Empty;
            }
        }

        public bool IsDefaultProfile
        {
            get
            {
                return true;
            }
        }

        IEnumerable<ConfigurationSection> IConfiguration.Sections
        {
            get
            {
                return this.Sections.Values;
            }
        }

        public IDictionary<string, ConfigurationSection> Sections { get; private set; }

        public IDictionary<string, ConfigurationElement> Elements { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Configuration.Saving += this.OnSaving;
            foreach (var section in this.Configuration.Sections)
            {
                this.WithSection(section);
            }
            base.InitializeComponent(core);
        }

        protected virtual void OnSaving(object sender, EventArgs e)
        {
            this.Save();
        }

        public void Load()
        {
            this.Load(this.Profile);
        }

        public void Load(string profile)
        {
            foreach (var pair in this.Sections)
            {
                if (pair.Value.IsInitialized)
                {
                    continue;
                }
                pair.Value.InitializeComponent();
            }
            this.OnLoading();
            foreach (var pair in this.Component.MetaData)
            {
                if (!pair.Key.StartsWith(PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var id = pair.Key.Substring(PREFIX.Length);
                var value = pair.Value;
                this.Load(id, value);
            }
            this.OnLoaded();
        }

        protected virtual void Load(string id, string value)
        {
            var element = default(ConfigurationElement);
            if (!this.Elements.TryGetValue(id, out element))
            {
                Logger.Write(this, LogLevel.Warn, "Configuration element \"{0}\" no longer exists.", id);
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Loading configuration element: \"{0}\".", id);
            element.SetPersistentValue(value);
        }

        protected virtual void OnLoading()
        {
            if (this.Loading == null)
            {
                return;
            }
            this.Loading(this, EventArgs.Empty);
        }

        public event EventHandler Loading;

        protected virtual void OnLoaded()
        {
            if (this.Loaded == null)
            {
                return;
            }
            this.Loaded(this, EventArgs.Empty);
        }

        public event EventHandler Loaded;

        protected virtual void OnSaving()
        {
            if (this.Saving == null)
            {
                return;
            }
            using (var e = OrderedEventArgs.Begin())
            {
                this.Saving(this, e);
            }
        }

        public event OrderedEventHandler Saving;

        protected virtual void OnSaved()
        {
            if (this.Saved == null)
            {
                return;
            }
            this.Saved(this, EventArgs.Empty);
        }

        public event EventHandler Saved;

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public void Delete(string profile)
        {
            throw new NotImplementedException();
        }

        public void ConnectDependencies()
        {
            foreach (var element in this.Elements.Values)
            {
                element.ConnectDependencies(this);
            }
        }

        public ConfigurationSection GetSection(string sectionId)
        {
            var section = default(ConfigurationSection);
            if (this.Sections.TryGetValue(sectionId, out section))
            {
                return section;
            }
            return default(ConfigurationSection);
        }

        public T GetElement<T>(string sectionId, string elementId) where T : ConfigurationElement
        {
            return this.GetElement(sectionId, elementId) as T;
        }

        public ConfigurationElement GetElement(string sectionId, string elementId)
        {
            var section = this.GetSection(sectionId);
            if (section == null)
            {
                return default(ConfigurationElement);
            }
            return section.GetElement(elementId);
        }

        public void Reset()
        {
            foreach (var pair in this.Elements)
            {
                pair.Value.Reset();
            }
        }

        public void Save()
        {
            this.Save(this.Profile);
        }

        public void Save(string profile)
        {
            this.OnSaving();
            Logger.Write(this, LogLevel.Debug, "Saving configuration.");
            try
            {
                foreach (var element in this.Elements.Values)
                {
                    var key = string.Concat(PREFIX, element.Id);
                    if (!element.IsModified)
                    {
                        var value = default(string);
                        if (this.Component.MetaData.TryRemove(key, out value))
                        {
                            Logger.Write(this, LogLevel.Debug, "Removing config: {0}", key);
                        }
                    }
                    else
                    {
                        var value = element.GetPersistentValue();
                        this.Component.MetaData.AddOrUpdate(key,
                            (_key) =>
                            {
                                Logger.Write(this, LogLevel.Debug, "Adding config: {0}", key);
                                return value;
                            },
                            (_key, _value) =>
                            {
                                if (!string.Equals(value, _value, StringComparison.OrdinalIgnoreCase))
                                {
                                    Logger.Write(this, LogLevel.Debug, "Updating config: {0}", key);
                                }
                                return value;
                            }
                        );
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to save configuration: {0}", e.Message);
            }
            this.OnSaved();
        }

        public IConfiguration WithSection(ConfigurationSection section)
        {
            if (this.Contains(section.Id))
            {
                this.Update(section);
            }
            else
            {
                this.Add(section);
            }
            return this;
        }

        public bool Contains(string id)
        {
            return this.GetSection(id) != null;
        }

        private void Add(ConfigurationSection section)
        {
            Logger.Write(this, LogLevel.Debug, "Adding configuration section: {0} => {1}", section.Id, section.Name);
            section = new ConfigurationSectionWrapper(this, section);
            this.Sections.Add(section.Id, section);
        }

        private void Update(ConfigurationSection section)
        {
            Logger.Write(this, LogLevel.Debug, "Updating configuration section: {0} => {1}", section.Id, section.Name);
            var existing = this.GetSection(section.Id);
            existing.Update(section);
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            //TODO: Sometimes, Dispose is called before OnSaving fires. Seems to happen more on faster systems.
            this.Save();
            if (this.Configuration != null)
            {
                this.Configuration.Saving -= this.OnSaving;
            }
        }

        ~UIComponentConfigurationProvider()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public class ConfigurationSectionWrapper : ConfigurationSection
        {
            public ConfigurationSectionWrapper(UIComponentConfigurationProvider provider, ConfigurationSection section) : base(section.Id, section.Name, section.Description)
            {
                this.Provider = provider;
                this.Elements.AddRange(section.Elements);
                if (this.Provider.IsInitialized)
                {
                    this.Provider.Elements.AddRange(section.Elements);
                }
                this.Flags = section.Flags;
            }

            public UIComponentConfigurationProvider Provider { get; private set; }

            protected override void Add(ConfigurationElement element)
            {
                if (this.Provider.IsInitialized)
                {
                    this.Provider.Elements.Add(element.Id, element);
                }
                base.Add(element);
            }
        }
    }
}
