using FoxTunes.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes
{
    public abstract class WindowBlurProvider : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        public abstract string Id { get; }

        public bool IsTransparencyEnabled
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key == null)
                    {
                        return false;
                    }
                    return Convert.ToBoolean(key.GetValue("EnableTransparency"));
                }
            }
        }

        public IConfigurationBase Configuration { get; private set; }

        public BooleanConfigurationElement Transparency { get; private set; }

        public SelectionConfigurationElement Provider { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            WindowBase.ActiveChanged += this.OnActiveChanged;
            this.Configuration = core.Components.Configuration;
            this.Transparency = this.Configuration.GetElement<BooleanConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.TRANSPARENCY
            );
            this.Provider = this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.TRANSPARENCY_PROVIDER
            );
            this.Transparency.ValueChanged += this.OnValueChanged;
            this.Provider.ValueChanged += this.OnValueChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnActiveChanged(object sender, EventArgs e)
        {
            var task = this.Refresh();
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            var task = this.Refresh();
        }

        public Task Refresh()
        {
            var enabled = this.Transparency.Value && string.Equals(this.Provider.Value.Id, this.Id, StringComparison.OrdinalIgnoreCase);
            if (!enabled)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.OnEnabled();
        }

        protected abstract Task OnEnabled();

        protected abstract Task OnDisabled();

        protected virtual async Task<bool> GetAllowsTransparency(Window window)
        {
            var allowsTransparency = default(bool);
            await Windows.Invoke(() => allowsTransparency = WindowExtensions.GetAllowsTransparency(window)).ConfigureAwait(false);
            return allowsTransparency;
        }

        public abstract IEnumerable<ConfigurationSection> GetConfigurationSections();

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
            WindowBase.ActiveChanged -= this.OnActiveChanged;
            if (this.Transparency != null)
            {
                this.Transparency.ValueChanged -= this.OnValueChanged;
            }
            if (this.Provider != null)
            {
                this.Provider.ValueChanged -= this.OnValueChanged;
            }
        }

        ~WindowBlurProvider()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
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
