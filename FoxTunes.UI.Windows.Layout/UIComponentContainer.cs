using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FoxTunes
{
    public class UIComponentContainer : DockPanel, IInvocableComponent, IUIComponent, IValueConverter, IConfigurationProvider
    {
        public const string REPLACE = "WWWW";

        public const string WRAP = "XXXX";

        public const string CLEAR = "YYYY";

        public static readonly UIComponentFactory Factory = ComponentRegistry.Instance.GetComponent<UIComponentFactory>();

        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static readonly DependencyProperty ConfigurationProperty = DependencyProperty.Register(
            "Configuration",
            typeof(UIComponentConfiguration),
            typeof(UIComponentContainer),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnConfigurationChanged))
       );

        public static UIComponentConfiguration GetConfiguration(UIComponentContainer source)
        {
            return (UIComponentConfiguration)source.GetValue(ConfigurationProperty);
        }

        public static void SetConfiguration(UIComponentContainer source, UIComponentConfiguration value)
        {
            source.SetValue(ConfigurationProperty, value);
        }

        public static void OnConfigurationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var componentContainer = sender as UIComponentContainer;
            if (componentContainer == null)
            {
                return;
            }
            if (e.OldValue is UIComponentConfiguration oldComponent && e.NewValue is UIComponentConfiguration newComponent)
            {
                if (!newComponent.Children.Any())
                {
                    if (newComponent.Children.Contains(oldComponent))
                    {
                        //Component was re-hosted (wrapped).
                    }
                    else
                    {
                        var children = default(IEnumerable<UIComponentConfiguration>);
                        switch (newComponent.Component.Children)
                        {
                            case UIComponent.NO_CHILDREN:
                                break;
                            case UIComponent.UNLIMITED_CHILDREN:
                                children = oldComponent.Children;
                                break;
                            default:
                                children = oldComponent.Children.Take(
                                    newComponent.Component.Children
                                );
                                break;
                        }
                        if (children != null)
                        {
                            newComponent.Children.AddRange(children);
                        }
                    }
                }
                if (!newComponent.MetaData.Any())
                {
                    newComponent.MetaData.TryAddRange(oldComponent.MetaData);
                }
                componentContainer.RaiseEvent(new RoutedPropertyChangedEventArgs<UIComponentConfiguration>(
                    oldComponent,
                    newComponent,
                    ConfigurationChangedEvent
                ));
            }
            componentContainer.OnConfigurationChanged();
        }

        public static readonly RoutedEvent ConfigurationChangedEvent = EventManager.RegisterRoutedEvent(
            "ConfigurationChanged",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<UIComponentConfiguration>),
            typeof(UIComponentContainer)
        );

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content",
            typeof(UIComponentBase),
            typeof(UIComponentContainer),
            new PropertyMetadata(new PropertyChangedCallback(OnContentChanged))
       );

        public static UIComponentBase GetContent(UIComponentContainer source)
        {
            return (UIComponentBase)source.GetValue(ContentProperty);
        }

        public static void SetContent(UIComponentContainer source, UIComponentBase value)
        {
            source.SetValue(ContentProperty, value);
        }

        public static void OnContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentContainer;
            if (container == null)
            {
                return;
            }
            container.OnContentChanged();
        }

        public UIComponentContainer()
        {
            this.InitializeComponent();
        }

        public ContentControl ContentControl { get; private set; }

        public Rectangle Rectangle { get; private set; }

        public UIComponentConfiguration Configuration
        {
            get
            {
                return GetConfiguration(this);
            }
            set
            {
                SetConfiguration(this, value);
            }
        }

        protected virtual void OnConfigurationChanged()
        {
            //Nothing to do.
        }

        public event RoutedPropertyChangedEventHandler<UIComponentConfiguration> ConfigurationChanged
        {
            add
            {
                AddHandler(ConfigurationChangedEvent, value);
            }
            remove
            {
                RemoveHandler(ConfigurationChangedEvent, value);
            }
        }

        public UIComponentBase Content
        {
            get
            {
                return this.GetValue(ContentProperty) as UIComponentBase;
            }
            set
            {
                this.SetValue(ContentProperty, value);
            }
        }

        protected virtual void OnContentChanged()
        {
            if (this.ContentChanged != null)
            {
                this.ContentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Content");
        }

        public event EventHandler ContentChanged;

        protected virtual void InitializeComponent()
        {
            this.Rectangle = new Rectangle();
            this.Rectangle.Fill = Brushes.Transparent;
            this.ContentControl = new ContentControl();
            this.Children.Add(this.Rectangle);
            this.Children.Add(this.ContentControl);
            this.Loaded += this.OnLoaded;
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.ContentControl.SetBinding(
                ContentControl.ContentProperty,
                new Binding()
                {
                    Path = new PropertyPath("Configuration"),
                    Source = this,
                    Converter = this
                }
            );
            this.Loaded -= this.OnLoaded;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (this.Content != null)
            {
                UIDisposer.Dispose(this.Content);
            }
            var configuration = value as UIComponentConfiguration;
            if (configuration == null || configuration.Component == null)
            {
                return this.CreateSelector();
            }
            var component = default(UIComponentBase);
            var control = Factory.CreateControl(configuration, out component);
            if (control == null)
            {
                //If a plugin was uninstalled the control will be null.
                return this.CreateSelector();
            }
            else if (component is ConfigurableUIComponentBase configurable)
            {
                configurable.Configuration = this.GetConfiguration(configurable);
            }
            this.Content = component;
            return control;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected virtual FrameworkElement CreateSelector()
        {
            var textBlock = new TextBlock();
            textBlock.Text = "Select Component";
            textBlock.IsHitTestVisible = false;
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.Margin = new Thickness(8, 0, 40, 0);
            textBlock.SetResourceReference(
                TextBlock.ForegroundProperty,
                "TextBrush"
            );

            var selector = new UIComponentSelector();
            selector.HorizontalAlignment = HorizontalAlignment.Stretch;
            selector.VerticalAlignment = VerticalAlignment.Center;
            selector.SetBinding(
                UIComponentSelector.ComponentProperty,
                new Binding()
                {
                    Source = this,
                    Path = new PropertyPath("Configuration")
                }
            );

            var grid = new Grid();
            grid.Children.Add(selector);
            grid.Children.Add(textBlock);

            return grid;
        }

        public void InitializeComponent(ICore core)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_GLOBAL;
            }
        }

        public virtual IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                var panel = this.FindAncestor<UIComponentPanel>();
                if (panel != null && !panel.IsEditable)
                {
                    yield break;
                }
                var attributes = InvocationComponent.ATTRIBUTE_SEPARATOR;
                if (!this.Configuration.Component.IsEmpty)
                {
                    foreach (var alternative in LayoutManager.Instance.GetComponents(this.Configuration.Component.Role))
                    {
                        if (string.Equals(this.Configuration.Component.Id, alternative.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_GLOBAL, REPLACE, alternative.Name, path: Strings.UIComponentContainer_Replace, attributes: attributes);
                        attributes = InvocationComponent.ATTRIBUTE_NONE;
                    }
                    foreach (var alternative in LayoutManager.Instance.GetComponents(UIComponentRole.Container))
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_GLOBAL, WRAP, alternative.Name, path: Strings.UIComponentContainer_Wrap, attributes: attributes);
                        attributes = InvocationComponent.ATTRIBUTE_NONE;
                    }
                }
                yield return new InvocationComponent(InvocationComponent.CATEGORY_GLOBAL, CLEAR, Strings.UIComponentContainer_Clear, attributes: attributes);
            }
        }

        public virtual Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case REPLACE:
                    return this.Replace(component.Name);
                case WRAP:
                    return this.Wrap(component.Name);
                case CLEAR:
                    return this.Clear();

            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task Replace(string name)
        {
            return Windows.Invoke(() =>
            {
                if (this.Configuration == null || this.Configuration.Component == null)
                {
                    return;
                }
                var component = LayoutManager.Instance.GetComponent(name, this.Configuration.Component.Role);
                if (component == null)
                {
                    return;
                }
                this.Configuration = new UIComponentConfiguration(component);
            });
        }

        public Task Wrap(string name)
        {
            return Windows.Invoke(() =>
            {
                if (this.Configuration == null || this.Configuration.Component == null)
                {
                    return;
                }
                var component = LayoutManager.Instance.GetComponent(name, UIComponentRole.Container);
                if (component == null)
                {
                    return;
                }
                this.Configuration = new UIComponentConfiguration(component, this.Configuration);
            });
        }

        public Task Clear()
        {
            return Windows.Invoke(() => this.Configuration = new UIComponentConfiguration());
        }

        public IConfiguration GetConfiguration(IConfigurableComponent component)
        {
            var configuration = new UIComponentConfigurationProvider(this.Configuration);
            configuration.InitializeComponent(Core.Instance);
            Logger.Write(this, LogLevel.Debug, "Registering configuration for component {0}.", component.GetType().Name);
            try
            {
                var sections = component.GetConfigurationSections();
                foreach (var section in sections)
                {
                    configuration.WithSection(section);
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to register configuration for component {0}: {1}", component.GetType().Name, e.Message);
            }
            configuration.Load();
            configuration.ConnectDependencies();
            return configuration;
        }

        protected virtual void Dispatch(Func<Task> function)
        {
#if NET40
            var task = TaskEx.Run(function);
#else
            var task = Task.Run(function);
#endif
        }

        protected virtual void OnPropertyChanging(string propertyName)
        {
            if (this.PropertyChanging == null)
            {
                return;
            }
            this.PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }

        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged == null)
            {
                return;
            }
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
