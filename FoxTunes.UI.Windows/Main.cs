using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public class Main : ContentControl, IDisposable
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }


        public Main()
        {
            LayoutManager.Instance.ProviderChanged += this.OnProviderChanged;
            this.LoadLayout();
        }

        public IUILayoutProvider Provider { get; private set; }

        protected virtual void OnProviderChanged(object sender, EventArgs e)
        {
            this.LoadLayout();
        }

        protected virtual void OnProviderUpdated(object sender, EventArgs e)
        {
            this.LoadLayout();
        }

        protected virtual Task LoadLayout()
        {
            if (this.Provider != null)
            {
                this.Provider.Updated -= this.OnProviderUpdated;
            }
            this.Provider = LayoutManager.Instance.Provider;
            if (this.Provider != null)
            {
                this.Provider.Updated += this.OnProviderUpdated;
            }
            return Windows.Invoke(() =>
            {
                if (this.Content is FrameworkElement element)
                {
                    UIDisposer.Dispose(element);
                }
                this.Content = LayoutManager.Instance.Load(UILayoutTemplate.Main);
            });
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
            LayoutManager.Instance.ProviderChanged -= this.OnProviderChanged;
            if (this.Provider != null)
            {
                this.Provider.Updated -= this.OnProviderUpdated;
            }
        }

        ~Main()
        {
            Logger.Write(typeof(Main), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
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
