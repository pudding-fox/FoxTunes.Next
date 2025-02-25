using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class LayoutPresetBehaviour : StandardBehaviour, IInvocableComponent, IDisposable
    {
        public const string LOAD = "AAAA";

        public bool Enabled
        {
            get
            {
                return UIComponentRoot.Active.Any();
            }
        }

        public IUserInterface UserInterface { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement MainPreset { get; private set; }

        public TextConfigurationElement MainLayout { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            this.Configuration = core.Components.Configuration;
            this.MainPreset = this.Configuration.GetElement<SelectionConfigurationElement>(
                UIComponentLayoutProviderConfiguration.SECTION,
                UIComponentLayoutProviderConfiguration.MAIN_PRESET
            );
            this.MainPreset.ValueChanged += this.OnValueChanged;
            this.MainLayout = this.Configuration.GetElement<TextConfigurationElement>(
                UIComponentLayoutProviderConfiguration.SECTION,
                UIComponentLayoutProviderConfiguration.MAIN_LAYOUT
            );
            base.InitializeComponent(core);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            if (this.MainPreset.Value == null)
            {
                return;
            }
            var preset = UIComponentLayoutProviderPresets.Instance.GetPresetById(
                this.MainPreset.Value.Id,
                UIComponentLayoutProviderPreset.CATEGORY_MAIN
            );
            if (preset == null)
            {
                return;
            }
            this.MainLayout.Value = preset.Layout;
            this.OnActivePresetChanged();
        }


        public IUIComponentLayoutProviderPreset ActivePreset
        {
            get
            {
                return UIComponentLayoutProviderPresets.Instance.GetPresetsByCategory(
                    UIComponentLayoutProviderPreset.CATEGORY_MAIN
                ).FirstOrDefault(UIComponentLayoutProviderPresets.Instance.IsLoaded);
            }
            set
            {
                if (value == null)
                {
                    return;
                }
                this.Load(value.Name);
            }
        }

        protected virtual void OnActivePresetChanged()
        {
            if (this.ActivePresetChanged != null)
            {
                this.ActivePresetChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ActivePreset");
        }

        public event EventHandler ActivePresetChanged;

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_SETTINGS;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Enabled)
                {
                    foreach (var option in this.MainPreset.Options.OrderBy(option => option.Name))
                    {
                        var preset = UIComponentLayoutProviderPresets.Instance.GetPresetById(
                            option.Id,
                            UIComponentLayoutProviderPreset.CATEGORY_MAIN
                        );
                        var isActive = UIComponentLayoutProviderPresets.Instance.IsLoaded(
                            preset
                        );
                        yield return new InvocationComponent(
                            InvocationComponent.CATEGORY_SETTINGS,
                            LOAD,
                            option.Name,
                            path: Path.Combine(Strings.LayoutDesignerBehaviour_Path, Strings.LayoutDesignerBehaviour_Load),
                            attributes: isActive ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                        );
                    }
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case LOAD:
                    return this.Load(component.Name);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task Load(string name)
        {
            //Using SetValue so the ValueChanged event fires even if we're updating to the same selected preset.
            this.MainPreset.SetValue(this.MainPreset.Options.FirstOrDefault(option => string.Equals(option.Name, name)));
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
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
            if (this.MainPreset != null)
            {
                this.MainPreset.ValueChanged += this.OnValueChanged;
            }
        }

        ~LayoutPresetBehaviour()
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
