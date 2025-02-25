using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes
{
    public class UIComponentShim : ContentControl
    {
        public static readonly DependencyProperty ComponentProperty = DependencyProperty.Register(
            "Component",
            typeof(string),
            typeof(UIComponentShim),
            new PropertyMetadata(new PropertyChangedCallback(OnComponentChanged))
        );

        public static string GetComponent(UIComponentShim source)
        {
            return (string)source.GetValue(ComponentProperty);
        }

        public static void SetComponent(UIComponentShim source, string value)
        {
            source.SetValue(ComponentProperty, value);
        }

        public static void OnComponentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var shim = sender as UIComponentShim;
            if (shim == null)
            {
                return;
            }
            shim.OnComponentChanged();
        }

        public string Component
        {
            get
            {
                return GetComponent(this);
            }
            set
            {
                SetComponent(this, value);
            }
        }

        protected virtual void OnComponentChanged()
        {
            this.Refresh();
            if (this.ComponentChanged != null)
            {
                this.ComponentChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler ComponentChanged;

        protected virtual void Refresh()
        {
            BindingOperations.ClearBinding(this, UIElement.VisibilityProperty);
            var type = this.GetComponentType(this.Component);
            if (type == null || type == LayoutManager.PLACEHOLDER)
            {
                //The component is not available.
                this.Content = null;
                this.Visibility = Visibility.Collapsed;
                return;
            }
            var component = ComponentActivator.Instance.Activate<UIComponentBase>(type);
            if (component == null)
            {
                //The component could not be loaded.
                this.Content = null;
                this.Visibility = Visibility.Collapsed;
                return;
            }
            this.Content = component;
            this.SetBinding(
                UIElement.VisibilityProperty,
                new Binding()
                {
                    Source = this.Content,
                    Path = new PropertyPath(nameof(UIComponentBase.IsComponentEnabled)),
                    Converter = new BooleanToVisibilityConverter()
                }
            );
        }

        protected virtual Type GetComponentType(string id)
        {
            var component = LayoutManager.Instance.GetComponent(id);
            if (component == null)
            {
                return LayoutManager.PLACEHOLDER;
            }
            return component.Type;
        }
    }
}
