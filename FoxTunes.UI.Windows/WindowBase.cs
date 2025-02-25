using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Linq;
using FoxTunes.Interfaces;
using FoxDb;

#if NET40
using Microsoft.Windows.Shell;
#else
using System.Windows.Shell;
#endif


namespace FoxTunes
{
    public abstract class WindowBase : Window, IUserInterfaceWindow
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        static WindowBase()
        {
            Instances = new List<WeakReference<WindowBase>>();
        }

        private static IList<WeakReference<WindowBase>> Instances { get; set; }

        public static IEnumerable<WindowBase> Active
        {
            get
            {
                lock (Instances)
                {
                    return Instances
                        .Where(instance => instance != null && instance.IsAlive)
                        .Select(instance => instance.Target)
                        .ToArray();
                }
            }
        }

        protected static void OnActiveChanged(WindowBase sender)
        {
            if (ActiveChanged == null)
            {
                return;
            }
            ActiveChanged(sender, EventArgs.Empty);
        }

        public static event EventHandler ActiveChanged;

        public WindowBase()
        {
            this.InitializeComponent();
        }

        new protected virtual bool ApplyTemplate
        {
            get
            {
                return true;
            }
        }

        protected virtual bool ApplyWindowChrome
        {
            get
            {
                return true;
            }
        }

        private void InitializeComponent()
        {
            this.WindowStyle = WindowStyle.None;
            this.Background = Brushes.Transparent;
            WindowExtensions.SetAllowsTransparency(this, true);
            WindowExtensions.SetFontFamily(this, true);
            if (this.ApplyTemplate)
            {
                this.ShowTemplate();
            }
            if (this.ApplyWindowChrome)
            {
                this.ShowWindowChrome();
            }
        }

        public virtual void ShowTemplate()
        {
            this.Template = TemplateFactory.Template;
        }

        public virtual void HideTemplate()
        {
            this.Template = TemplateFactory.DefaultTemplate;
        }

        public virtual void ShowWindowChrome()
        {
            this.SetValue(WindowChrome.WindowChromeProperty, new WindowChrome()
            {
                CaptionHeight = 30,
                ResizeBorderThickness = new Thickness(5)
            });
        }

        public virtual void HideWindowChrome()
        {
            this.SetValue(WindowChrome.WindowChromeProperty, new WindowChrome()
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(0)
            });
        }

        protected override void OnContentRendered(EventArgs e)
        {
            if (this.Handle == IntPtr.Zero)
            {
                this.Handle = this.GetHandle();
                lock (Instances)
                {
                    Instances.Add(new WeakReference<WindowBase>(this));
                }
                OnActiveChanged(this);
                OnCreated(this);
            }
            base.OnContentRendered(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            lock (Instances)
            {
                for (var a = Instances.Count - 1; a >= 0; a--)
                {
                    var instance = Instances[a];
                    if (instance == null || !instance.IsAlive)
                    {
                        Instances.RemoveAt(a);
                    }
                    else if (object.ReferenceEquals(this, instance.Target))
                    {
                        Instances.RemoveAt(a);
                    }
                }
            }
            OnActiveChanged(this);
            OnDestroyed(this);
            base.OnClosed(e);
        }

        protected static void OnCreated(WindowBase sender)
        {
            if (Created != null)
            {
                Created(sender, EventArgs.Empty);
            }
        }

        public static event EventHandler Created;

        protected static void OnDestroyed(WindowBase sender)
        {
            UIDisposer.Dispose(sender);
            if (Destroyed != null)
            {
                Destroyed(sender, EventArgs.Empty);
            }
        }

        public static event EventHandler Destroyed;

        #region IUserInterfaceWindow

        public abstract string Id { get; }

        public virtual IntPtr Handle { get; private set; }

        public virtual UserInterfaceWindowRole Role
        {
            get
            {
                return UserInterfaceWindowRole.None;
            }
        }

        #endregion

        private static class TemplateFactory
        {
            private static Lazy<ControlTemplate> _Template = new Lazy<ControlTemplate>(GetTemplate);

            private static Lazy<ControlTemplate> _DefaultTemplate = new Lazy<ControlTemplate>(GetDefaultTemplate);

            public static ControlTemplate Template
            {
                get
                {
                    return _Template.Value;
                }
            }

            public static ControlTemplate DefaultTemplate
            {
                get
                {
                    return _DefaultTemplate.Value;
                }
            }

            private static ControlTemplate GetTemplate()
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(global::FoxTunes.Resources.WindowBase)))
                {
                    var template = (ControlTemplate)XamlReader.Load(stream);
                    template.Seal();
                    return template;
                }
            }

            private static ControlTemplate GetDefaultTemplate()
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(global::FoxTunes.Resources.WindowBaseMinimal)))
                {
                    var template = (ControlTemplate)XamlReader.Load(stream);
                    template.Seal();
                    return template;
                }
            }
        }
    }
}
