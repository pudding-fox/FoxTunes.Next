using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace FoxTunes.ViewModel.Config
{
    public class SelectionConfigurationElement : ViewModelBase
    {
        public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(
           "Element",
           typeof(global::FoxTunes.SelectionConfigurationElement),
           typeof(SelectionConfigurationElement),
           new PropertyMetadata(new PropertyChangedCallback(OnElementChanged))
       );

        public static global::FoxTunes.SelectionConfigurationElement GetElement(ViewModelBase source)
        {
            return (global::FoxTunes.SelectionConfigurationElement)source.GetValue(ElementProperty);
        }

        public static void SetElement(ViewModelBase source, global::FoxTunes.SelectionConfigurationElement value)
        {
            source.SetValue(ElementProperty, value);
        }

        public static void OnElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = sender as SelectionConfigurationElement;
            if (viewModel == null)
            {
                return;
            }
            viewModel.OnElementChanged();
        }

        public global::FoxTunes.SelectionConfigurationElement Element
        {
            get
            {
                return this.GetValue(ElementProperty) as global::FoxTunes.SelectionConfigurationElement;
            }
            set
            {
                this.SetValue(ElementProperty, value);
            }
        }

        protected virtual void OnElementChanged()
        {
            this.OnOptionsChanged();
            if (this.ElementChanged != null)
            {
                this.ElementChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Element");
        }

        public event EventHandler ElementChanged;

        public IEnumerable<SelectionConfigurationOption> Options
        {
            get
            {
                if (this.Element == null)
                {
                    return Enumerable.Empty<SelectionConfigurationOption>();
                }
                return this.Element.Options.OrderBy(option => option.Name, NumericFallbackComparer.Instance);
            }
        }

        protected virtual void OnOptionsChanged()
        {
            if (this.OptionsChanged != null)
            {
                this.OptionsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Options");
        }

        public event EventHandler OptionsChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new SelectionConfigurationElement();
        }
    }
}
