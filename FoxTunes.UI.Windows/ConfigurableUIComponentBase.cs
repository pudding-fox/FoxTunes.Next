using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static FoxTunes.UIElementExtensions;

namespace FoxTunes
{
    public abstract class ConfigurableUIComponentBase : UIComponentBase, IConfigurableComponent, IInvocableComponent, IConfigurationTarget, IDisposable
    {
        public const string SETTINGS = "ZZZZ";

        public ConfigurableUIComponentBase()
        {
            this.Loaded += this.OnLoaded;
        }

        protected virtual void CreateMenu()
        {
            var menu = new Menu()
            {
                Components = new ObservableCollection<IInvocableComponent>()
                {
                    this
                }
            };
            this.ContextMenu = menu;
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.CreateMenu();
            this.ApplyConfiguration();
        }

        protected virtual void ApplyConfiguration()
        {
            this.ApplyConfiguration(this);
            var children = this.FindChildren<IConfigurationTarget>();
            foreach (var child in children)
            {
                if (object.ReferenceEquals(this, child))
                {
                    //Not sure if this is a bug or intended behaviour but FindChildren returns the parent (this).
                    continue;
                }
                if (!object.ReferenceEquals(child.Configuration, this.Configuration))
                {
                    child.Configuration = this.Configuration;
                }
                if (child is FrameworkElement element)
                {
                    this.ApplyConfiguration(element);
                }
            }
        }

        protected virtual void ApplyConfiguration(FrameworkElement element)
        {
            if (element.Resources == null)
            {
                return;
            }
            foreach (var configurationTarget in element.Resources.Values.OfType<IConfigurationTarget>())
            {
                if (!object.ReferenceEquals(configurationTarget.Configuration, this.Configuration))
                {
                    configurationTarget.Configuration = this.Configuration;
                }
            }
        }

        public IUserInterface UserInterface { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            base.InitializeComponent(core);
        }

        private IConfiguration _Configuration { get; set; }

        public IConfiguration Configuration
        {
            get
            {
                return this._Configuration;
            }
            set
            {
                this._Configuration = value;
                this.OnConfigurationChanged();
            }
        }

        protected virtual void OnConfigurationChanged()
        {
            if (this.ConfigurationChanged != null)
            {
                this.ConfigurationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Configuration");
        }

        public event EventHandler ConfigurationChanged;

        public abstract IEnumerable<string> InvocationCategories { get; }

        public virtual IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                var invocationCategory = this.InvocationCategories.FirstOrDefault();
                if (!string.IsNullOrEmpty(invocationCategory))
                {
                    yield return new InvocationComponent(
                        invocationCategory,
                        SETTINGS,
                        StringResources.General_Settings,
                        attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                    );
                }
            }
        }

        public virtual Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(component.Id, SETTINGS, StringComparison.OrdinalIgnoreCase))
            {
                return this.ShowSettings();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected abstract Task<bool> ShowSettings();

        protected virtual Task<bool> ShowSettings(string title, string section)
        {
            return this.ShowSettings(title, new[] { section });
        }

        protected virtual Task<bool> ShowSettings(string title, IEnumerable<string> sections)
        {
            return this.UserInterface.ShowSettings(
                title,
                this.Configuration,
                sections
            );
        }

        public abstract IEnumerable<ConfigurationSection> GetConfigurationSections();

        protected override void OnDisposing()
        {
            this.Loaded -= this.OnLoaded;
            if (this.Configuration is IDisposable disposable)
            {
                disposable.Dispose();
            }
            base.OnDisposing();
        }
    }
}
