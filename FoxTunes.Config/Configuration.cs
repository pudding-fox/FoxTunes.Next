using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    [Component("BA77B392-1900-4931-B720-16206B23DDA1", ComponentSlots.Configuration)]
    public class Configuration : StandardComponent, IConfiguration
    {
        public Configuration()
        {
            this.Sections = new Dictionary<string, ConfigurationSection>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> AvailableProfiles
        {
            get
            {
                return Profiles.AvailableProfiles;
            }
        }

        public string Profile
        {
            get
            {
                return Profiles.Profile;
            }
        }

        public bool IsDefaultProfile
        {
            get
            {
                return string.Equals(this.Profile, Strings.Profiles_Default, StringComparison.OrdinalIgnoreCase);
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
            this.Sections.Add(section.Id, section);
        }

        private void Update(ConfigurationSection section)
        {
            Logger.Write(this, LogLevel.Debug, "Updating configuration section: {0} => {1}", section.Id, section.Name);
            var existing = this.GetSection(section.Id);
            existing.Update(section);
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
            var fileName = Profiles.GetFileName(profile);
            if (!string.Equals(Profiles.Profile, profile, StringComparison.OrdinalIgnoreCase))
            {
                //Switching profile, ensure the current one is saved.
                this.Save(Profiles.Profile);
            }
            if (!File.Exists(fileName))
            {
                Logger.Write(this, LogLevel.Debug, "Configuration file \"{0}\" does not exist.", fileName);
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Loading configuration from file \"{0}\".", fileName);
            this.OnLoading();
            try
            {
                var modifiedElements = this.GetModifiedElements();
                var restoredElements = new List<ConfigurationElement>();
                using (var stream = File.OpenRead(fileName))
                {
                    var sections = Serializer.Load(stream);
                    foreach (var section in sections)
                    {
                        if (!this.Contains(section.Key))
                        {
                            //If config was created by a component that is no longer loaded then it will be lost here.
                            //TODO: Add the config but hide it so it's preserved but not displayed.
                            Logger.Write(this, LogLevel.Warn, "Configuration section \"{0}\" no longer exists.", section.Key);
                            continue;
                        }
                        var existing = this.GetSection(section.Key);
                        try
                        {
                            Logger.Write(this, LogLevel.Debug, "Loading configuration section \"{0}\".", section.Key);
                            restoredElements.AddRange(this.Load(existing, section.Value));
                        }
                        catch (Exception e)
                        {
                            Logger.Write(this, LogLevel.Warn, "Failed to load configuration section \"{0}\": {1}", existing.Id, e.Message);
                        }
                    }
                }
                foreach (var modifiedElement in modifiedElements)
                {
                    if (restoredElements.Contains(modifiedElement))
                    {
                        continue;
                    }
                    Logger.Write(this, LogLevel.Debug, "Resetting configuration element: \"{0}\".", modifiedElement.Id);
                    modifiedElement.Reset();
                }
                Profiles.Profile = profile;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to load configuration: {0}", e.Message);
            }
            this.OnLoaded();
        }

        protected virtual IEnumerable<ConfigurationElement> Load(ConfigurationSection section, IEnumerable<KeyValuePair<string, string>> elements)
        {
            var restoredElements = new List<ConfigurationElement>();
            foreach (var element in elements)
            {
                if (!section.Contains(element.Key))
                {
                    //If config was created by a component that is no longer loaded then it will be lost here.
                    //TODO: Add the config but hide it so it's preserved but not displayed.
                    Logger.Write(this, LogLevel.Warn, "Configuration element \"{0}\" no longer exists.", element.Key);
                    continue;
                }
                Logger.Write(this, LogLevel.Debug, "Loading configuration element: \"{0}\".", element.Key);
                var existing = section.GetElement(element.Key);
                existing.SetPersistentValue(element.Value);
                restoredElements.Add(existing);
            }
            return restoredElements;
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

        public void Save()
        {
            this.Save(this.Profile);
        }

        public void Save(string profile)
        {
            if (!string.Equals(Profiles.Profile, profile, StringComparison.OrdinalIgnoreCase))
            {
                //Switching profile, copy the current one.
                this.Copy(profile);
                return;
            }
            var fileName = Profiles.GetFileName(profile);
            this.OnSaving();
            Logger.Write(this, LogLevel.Debug, "Saving configuration to file \"{0}\".", fileName);
            try
            {
                //Use a temp file so the settings aren't lost if something goes wrong.
                var temp = Path.GetTempFileName();
                using (var stream = File.Create(temp))
                {
                    Serializer.Save(stream, this.Sections.Values);
                }
                if (!MoveFileEx(temp, fileName, MOVEFILE_COPY_ALLOWED | MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
                {
                    throw new Exception("MoveFileEx: Failed.");
                }
                Profiles.Profile = profile;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to save configuration: {0}", e.Message);
            }
            this.OnSaved();
        }

        public void Copy(string profile)
        {
            if (string.Equals(Profiles.Profile, profile, StringComparison.OrdinalIgnoreCase))
            {
                //Nothing to do.
                return;
            }
            this.Save();
            try
            {
                var sourceFileName = Profiles.GetFileName(Profiles.Profile);
                var targetFileName = Profiles.GetFileName(profile);
                if (File.Exists(sourceFileName))
                {
                    File.Copy(sourceFileName, targetFileName);
                }
                Profiles.Profile = profile;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to copy configuration: {0}", e.Message);
            }
        }

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
            this.Delete(this.Profile);
        }

        public void Delete(string profile)
        {
            var fileName = Profiles.GetFileName(profile);
            try
            {
                Profiles.Delete(profile);
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                this.Load();
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to delete configuration: {0}", e.Message);
            }
            this.OnSaved();
        }

        public void Reset()
        {
            foreach (var pair in this.Sections)
            {
                pair.Value.Reset();
            }
        }

        public void ConnectDependencies()
        {
            foreach (var pair1 in this.Sections)
            {
                foreach (var pair2 in pair1.Value.Elements)
                {
                    pair2.Value.ConnectDependencies(this);
                }
            }
        }

        protected virtual IEnumerable<ConfigurationElement> GetModifiedElements()
        {
            var elements = new List<ConfigurationElement>();
            if (!string.IsNullOrEmpty(this.Profile))
            {
                foreach (var pair1 in this.Sections)
                {
                    foreach (var pair2 in pair1.Value.Elements)
                    {
                        if (!pair2.Value.IsModified)
                        {
                            continue;
                        }
                        elements.Add(pair2.Value);
                    }
                }
            }
            return elements;
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

        const int MOVEFILE_REPLACE_EXISTING = 0x1;

        const int MOVEFILE_COPY_ALLOWED = 0x2;

        const int MOVEFILE_WRITE_THROUGH = 0x8;

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);
    }
}
