using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    [ComponentPreference(ReleaseType.Default)]
    public class UIComponentLayoutProvider : UILayoutProviderBase, IConfigurableComponent, IDisposable
    {
        public const string ID = "CCCC083C-3C19-4AAC-97C7-565AF8F83115";

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        public override string Name
        {
            get
            {
                return Strings.UIComponentLayoutProvider_Name;
            }
        }

        public override string Description
        {
            get
            {
                return Strings.UIComponentLayoutProvider_Description;
            }
        }

        public IConfiguration Configuration { get; private set; }

        public TextConfigurationElement Main { get; private set; }

        private UIComponentConfiguration _MainComponent { get; set; }

        public UIComponentConfiguration MainComponent
        {
            get
            {
                return this._MainComponent;
            }
            set
            {
                this._MainComponent = value;
                this.OnMainComponentChanged();
            }
        }

        protected virtual void OnMainComponentChanged()
        {
            if (this.MainComponentChanged != null)
            {
                this.MainComponentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MainComponent");
        }

        public event EventHandler MainComponentChanged;

        public bool IsLoaded { get; private set; }

        public bool IsSaving { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Configuration.Loaded += this.OnLoaded;
            this.Configuration.Saving += this.OnSaving;
            this.Main = this.Configuration.GetElement<TextConfigurationElement>(
                UIComponentLayoutProviderConfiguration.SECTION,
                UIComponentLayoutProviderConfiguration.MAIN_LAYOUT
            );
            this.Main.ValueChanged += this.OnValueChanged;
            if (this.Active)
            {
                var task = this.Load();
            }
            base.InitializeComponent(core);
        }

        protected virtual void OnLoaded(object sender, EventArgs e)
        {
            if (this.Active)
            {
                var task = this.Load();
            }
        }

        protected virtual void OnSaving(object sender, OrderedEventArgs e)
        {
            if (!this.IsLoaded)
            {
                return;
            }
            e.Add(this.Save, OrderedEventArgs.PRIORITY_LOW);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            if (this.Active)
            {
                if (this.IsSaving)
                {
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Layout was modified, reloading.");
                var task = this.Load();
            }
        }

        protected virtual Task Load()
        {
            if (!this.HasChanges())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            this.IsLoaded = true;
            return Windows.Invoke(() =>
            {
                if (string.IsNullOrEmpty(this.Main.Value))
                {
                    Logger.Write(this, LogLevel.Debug, "No component to load.");
                    this.MainComponent = null;
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Loading component..");
                try
                {
                    using (var stream = new MemoryStream(Encoding.Default.GetBytes(this.Main.Value)))
                    {
                        this.MainComponent = Serializer.LoadComponent(stream);
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to load component: {0}", e.Message);
                }
            });
        }

        public override UIComponentBase Load(UILayoutTemplate template)
        {
            switch (template)
            {
                case UILayoutTemplate.Main:
                    if (!this.IsLoaded)
                    {
                        //TODO: Bad .Wait()
                        this.Load().Wait();
                    }
                    var root = new UIComponentRoot();
                    root.SetBinding(
                        UIComponentPanel.ConfigurationProperty,
                        new Binding()
                        {
                            Source = this,
                            Path = new PropertyPath("MainComponent")
                        }
                    );
                    return root;
            }
            throw new NotImplementedException();
        }

        protected virtual bool HasChanges()
        {
            var value = default(string);
            return this.HasChanges(out value);
        }

        protected virtual bool HasChanges(out string value)
        {
            if (this.MainComponent == null)
            {
                value = null;
                return !string.IsNullOrEmpty(this.Main.Value);
            }
            using (var stream = new MemoryStream())
            {
                Serializer.Save(stream, this.MainComponent);
                value = Encoding.Default.GetString(stream.ToArray());
            }
            return !string.Equals(this.Main.Value, value, StringComparison.OrdinalIgnoreCase);
        }

        protected virtual void Save()
        {
            if (this.Configuration == null || this.Main == null)
            {
                return;
            }
            try
            {
                var value = default(string);
                if (!this.HasChanges(out value))
                {
                    //Nothing to do.
                    return;
                }
                this.IsSaving = true;
                Logger.Write(this, LogLevel.Debug, "Saving config.");
                try
                {
                    this.Main.Value = value;
                }
                finally
                {
                    this.IsSaving = false;
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to save config: {0}", e.Message);
            }
        }

        public override UIComponentBase PresetSelector
        {
            get
            {
                return new LayoutSelector()
                {
                    IsEditable = false
                };
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return UIComponentLayoutProviderConfiguration.GetConfigurationSections();
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
            if (this.Configuration != null)
            {
                this.Configuration.Loaded -= this.OnLoaded;
                this.Configuration.Saving -= this.OnSaving;
            }
            if (this.Main != null)
            {
                this.Main.ValueChanged -= this.OnValueChanged;
            }
        }

        ~UIComponentLayoutProvider()
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
    }
}
