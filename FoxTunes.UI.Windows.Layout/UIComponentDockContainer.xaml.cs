using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for UIComponentDockContainer.xaml
    /// </summary>
    [UIComponent("3E899F79-380C-4EF7-8570-4B4E3B3467CB", children: 2, role: UIComponentRole.Container)]
    public partial class UIComponentDockContainer : UIComponentPanel
    {
        const string DOCK_TOP = "AAAA";

        const string DOCK_BOTTOM = "BBBB";

        const string DOCK_LEFT = "CCCC";

        const string DOCK_RIGHT = "DDDD";

        const string COLLAPSE = "EEEE";

        public static readonly DependencyProperty ContentComponentProperty = DependencyProperty.Register(
            "ContentComponent",
            typeof(UIComponentConfiguration),
            typeof(UIComponentDockContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnContentComponentChanged))
        );

        public static UIComponentConfiguration GetContentComponent(UIComponentDockContainer source)
        {
            return (UIComponentConfiguration)source.GetValue(ContentComponentProperty);
        }

        public static void SetContentComponent(UIComponentDockContainer source, UIComponentConfiguration value)
        {
            source.SetValue(ContentComponentProperty, value);
        }

        public static void OnContentComponentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentDockContainer;
            if (container == null)
            {
                return;
            }
            container.OnContentComponentChanged();
        }

        public static readonly DependencyProperty DockComponentProperty = DependencyProperty.Register(
            "DockComponent",
            typeof(UIComponentConfiguration),
            typeof(UIComponentDockContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnDockComponentChanged))
        );

        public static UIComponentConfiguration GetDockComponent(UIComponentDockContainer source)
        {
            return (UIComponentConfiguration)source.GetValue(DockComponentProperty);
        }

        public static void SetDockComponent(UIComponentDockContainer source, UIComponentConfiguration value)
        {
            source.SetValue(DockComponentProperty, value);
        }

        public static void OnDockComponentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentDockContainer;
            if (container == null)
            {
                return;
            }
            container.OnDockComponentChanged();
        }

        public static readonly DependencyProperty DockEnabledProperty = DependencyProperty.Register(
            "DockEnabled",
            typeof(bool),
            typeof(UIComponentDockContainer),
            new PropertyMetadata(true, new PropertyChangedCallback(OnDockEnabledChanged))
        );

        public static bool GetDockEnabled(UIComponentDockContainer source)
        {
            return (bool)source.GetValue(DockEnabledProperty);
        }

        public static void SetDockEnabled(UIComponentDockContainer source, bool value)
        {
            source.SetValue(DockEnabledProperty, value);
        }

        public static void OnDockEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentDockContainer;
            if (container == null)
            {
                return;
            }
            container.OnDockEnabledChanged();
        }

        public static readonly DependencyProperty DockLocationProperty = DependencyProperty.Register(
           "DockLocation",
           typeof(string),
           typeof(UIComponentDockContainer),
           new PropertyMetadata(new PropertyChangedCallback(OnDockLocationChanged))
       );

        public static string GetDockLocation(UIComponentDockContainer source)
        {
            return (string)source.GetValue(DockLocationProperty);
        }

        public static void SetDockLocation(UIComponentDockContainer source, string value)
        {
            source.SetValue(DockLocationProperty, value);
        }

        public static void OnDockLocationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentDockContainer;
            if (container == null)
            {
                return;
            }
            container.OnDockLocationChanged();
        }

        public static readonly DependencyProperty CollapseProperty = DependencyProperty.Register(
            "Collapse",
            typeof(bool),
            typeof(UIComponentDockContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnCollapseChanged))
        );

        public static bool GetCollapse(UIComponentDockContainer source)
        {
            return (bool)source.GetValue(CollapseProperty);
        }

        public static void SetCollapse(UIComponentDockContainer source, bool value)
        {
            source.SetValue(CollapseProperty, value);
        }

        public static void OnCollapseChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentDockContainer;
            if (container == null)
            {
                return;
            }
            container.OnCollapseChanged();
        }

        public UIComponentDockContainer()
        {
            this.InitializeComponent();
            this.SetBinding(
                UIComponentDockContainer.DockEnabledProperty,
                new Binding()
                {
                    Source = this.DockContainer,
                    Path = new PropertyPath("Content.IsComponentEnabled"),
                    FallbackValue = true
                }
            );
        }

        protected override void OnConfigurationChanged()
        {
            this.UpdateMetaData();
            this.UpdateChildren();
            base.OnConfigurationChanged();
        }

        protected virtual void UpdateChildren()
        {
            while (this.Configuration.Children.Count < 2)
            {
                this.Configuration.Children.Add(new UIComponentConfiguration());
            }
            this.ContentComponent = this.Configuration.Children[0];
            this.DockComponent = this.Configuration.Children[1];
        }

        protected virtual void UpdateMetaData()
        {
            var dockLocation = default(string);
            var collapse = default(string);
            if (this.Configuration.MetaData.TryGetValue(nameof(this.DockLocation), out dockLocation))
            {
                this.DockLocation = dockLocation;
            }
            else
            {
                this.DockLocation = Enum.GetName(typeof(Dock), Dock.Top);
            }
            if (this.Configuration.MetaData.TryGetValue(nameof(this.Collapse), out collapse))
            {
                this.Collapse = Convert.ToBoolean(collapse);
            }
            else
            {
                this.Collapse = false;
            }
        }

        public UIComponentConfiguration ContentComponent
        {
            get
            {
                return this.GetValue(ContentComponentProperty) as UIComponentConfiguration;
            }
            set
            {
                this.SetValue(ContentComponentProperty, value);
            }
        }

        protected virtual void OnContentComponentChanged()
        {
            this.Configuration.Children[0] = this.ContentComponent;
            if (this.ContentComponentChanged != null)
            {
                this.ContentComponentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ContentComponent");
        }

        public event EventHandler ContentComponentChanged;

        public UIComponentConfiguration DockComponent
        {
            get
            {
                return this.GetValue(DockComponentProperty) as UIComponentConfiguration;
            }
            set
            {
                this.SetValue(DockComponentProperty, value);
            }
        }

        protected virtual void OnDockComponentChanged()
        {
            this.Configuration.Children[1] = this.DockComponent;
            if (this.DockComponentChanged != null)
            {
                this.DockComponentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("DockComponent");
        }

        public event EventHandler DockComponentChanged;

        public bool DockEnabled
        {
            get
            {
                return (bool)this.GetValue(DockEnabledProperty);
            }
            set
            {
                this.SetValue(DockEnabledProperty, value);
            }
        }

        protected virtual void OnDockEnabledChanged()
        {
            if (this.DockEnabledChanged != null)
            {
                this.DockEnabledChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("DockEnabled");
        }

        public event EventHandler DockEnabledChanged;

        public string DockLocation
        {
            get
            {
                return (string)this.GetValue(DockLocationProperty);
            }
            set
            {
                this.SetValue(DockLocationProperty, value);
            }
        }

        protected virtual void OnDockLocationChanged()
        {
            this.Configuration.MetaData.AddOrUpdate(
                nameof(this.DockLocation),
                this.DockLocation
            );
            if (this.DockLocationChanged != null)
            {
                this.DockLocationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("DockLocation");
        }

        public event EventHandler DockLocationChanged;

        public bool Collapse
        {
            get
            {
                return (bool)this.GetValue(CollapseProperty);
            }
            set
            {
                this.SetValue(CollapseProperty, value);
            }
        }

        protected virtual void OnCollapseChanged()
        {
            this.Configuration.MetaData.AddOrUpdate(
                nameof(this.Collapse),
                Convert.ToString(this.Collapse)
            );
            if (this.CollapseChanged != null)
            {
                this.CollapseChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Collapse");
        }

        public event EventHandler CollapseChanged;

        public override IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_GLOBAL;
            }
        }

        public override IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    DOCK_TOP,
                    Strings.UIComponentDockContainer_DockTop,
                    attributes: string.Equals(this.DockLocation, Enum.GetName(typeof(Dock), Dock.Top), StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    DOCK_BOTTOM,
                    Strings.UIComponentDockContainer_DockBottom,
                    attributes: string.Equals(this.DockLocation, Enum.GetName(typeof(Dock), Dock.Bottom), StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    DOCK_LEFT,
                    Strings.UIComponentDockContainer_DockLeft,
                    attributes: string.Equals(this.DockLocation, Enum.GetName(typeof(Dock), Dock.Left), StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    DOCK_RIGHT,
                    Strings.UIComponentDockContainer_DockRight,
                    attributes: string.Equals(this.DockLocation, Enum.GetName(typeof(Dock), Dock.Right), StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    COLLAPSE,
                    Strings.UIComponentDockContainer_Collapse,
                    attributes: (byte)((this.Collapse ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
                );
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case DOCK_TOP:
                    return Windows.Invoke(() => this.DockLocation = Enum.GetName(typeof(Dock), Dock.Top));
                case DOCK_BOTTOM:
                    return Windows.Invoke(() => this.DockLocation = Enum.GetName(typeof(Dock), Dock.Bottom));
                case DOCK_LEFT:
                    return Windows.Invoke(() => this.DockLocation = Enum.GetName(typeof(Dock), Dock.Left));
                case DOCK_RIGHT:
                    return Windows.Invoke(() => this.DockLocation = Enum.GetName(typeof(Dock), Dock.Right));
                case COLLAPSE:
                    return Windows.Invoke(() => this.Collapse = !this.Collapse);
            }
            return base.InvokeAsync(component);
        }
    }
}
