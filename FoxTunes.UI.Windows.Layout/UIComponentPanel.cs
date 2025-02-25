using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FoxTunes
{
    public abstract class UIComponentPanel : UIComponentBase, IUIComponentPanel
    {
        public virtual bool IsEditable
        {
            get
            {
                return true;
            }
        }

        public static readonly DependencyProperty IsInDesignModeProperty = DependencyProperty.Register(
            "IsInDesignMode",
            typeof(bool),
            typeof(UIComponentPanel),
            new PropertyMetadata(new PropertyChangedCallback(OnIsInDesignModeChanged))
        );

        public static bool GetIsInDesignMode(UIComponentPanel source)
        {
            return (bool)source.GetValue(IsInDesignModeProperty);
        }

        public static void SetIsInDesignMode(UIComponentPanel source, bool value)
        {
            source.SetValue(IsInDesignModeProperty, value);
        }

        public static void OnIsInDesignModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentPanel;
            if (container == null)
            {
                return;
            }
            container.OnIsInDesignModeChanged();
        }

        public bool IsInDesignMode
        {
            get
            {
                return (bool)this.GetValue(IsInDesignModeProperty);
            }
            set
            {
                this.SetValue(IsInDesignModeProperty, value);
            }
        }

        protected virtual void OnIsInDesignModeChanged()
        {
            if (this.IsInDesignModeChanged != null)
            {
                this.IsInDesignModeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsInDesignMode");
        }

        public event EventHandler IsInDesignModeChanged;

        public static readonly DependencyProperty ConfigurationProperty = DependencyProperty.Register(
            "Configuration",
            typeof(UIComponentConfiguration),
            typeof(UIComponentPanel),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnConfigurationChanged))
        );

        public static UIComponentConfiguration GetConfiguration(UIComponentPanel source)
        {
            return (UIComponentConfiguration)source.GetValue(ConfigurationProperty);
        }

        public static void SetConfiguration(UIComponentPanel source, UIComponentConfiguration value)
        {
            source.SetValue(ConfigurationProperty, value);
        }

        public static void OnConfigurationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentPanel;
            if (container == null)
            {
                return;
            }
            container.OnConfigurationChanged();
        }

        public UIComponentConfiguration Configuration
        {
            get
            {
                return this.GetValue(ConfigurationProperty) as UIComponentConfiguration;
            }
            set
            {
                this.SetValue(ConfigurationProperty, value);
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

        protected UIComponentPanel()
        {
            this.CreateBindings();
        }

        protected virtual void CreateBindings()
        {
            this.SetBinding(
                IsInDesignModeProperty,
                new Binding()
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(UIComponentPanel), 1),
                    Path = new PropertyPath(nameof(this.IsInDesignMode))
                }
            );
        }

        public virtual IEnumerable<string> InvocationCategories
        {
            get
            {
                return Enumerable.Empty<string>();
            }
        }

        public virtual IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                return Enumerable.Empty<IInvocationComponent>();
            }
        }

        public virtual Task InvokeAsync(IInvocationComponent component)
        {
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
