using System;
using System.Linq;
using System.Windows;

namespace FoxTunes.ViewModel.Config
{
    public class DoubleConfigurationElement : ViewModelBase
    {
        public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(
           "Element",
           typeof(global::FoxTunes.DoubleConfigurationElement),
           typeof(DoubleConfigurationElement),
           new PropertyMetadata(new PropertyChangedCallback(OnElementChanged))
       );

        public static global::FoxTunes.DoubleConfigurationElement GetElement(ViewModelBase source)
        {
            return (global::FoxTunes.DoubleConfigurationElement)source.GetValue(ElementProperty);
        }

        public static void SetElement(ViewModelBase source, global::FoxTunes.DoubleConfigurationElement value)
        {
            source.SetValue(ElementProperty, value);
        }

        public static void OnElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = sender as DoubleConfigurationElement;
            if (viewModel == null)
            {
                return;
            }
            viewModel.OnElementChanged();
        }

        public global::FoxTunes.DoubleConfigurationElement Element
        {
            get
            {
                return this.GetValue(ElementProperty) as global::FoxTunes.DoubleConfigurationElement;
            }
            set
            {
                this.SetValue(ElementProperty, value);
            }
        }

        protected virtual void OnElementChanged()
        {
            this.OnMinValueChanged();
            this.OnMaxValueChanged();
            this.OnStepChanged();
            if (this.ElementChanged != null)
            {
                this.ElementChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Element");
        }

        public event EventHandler ElementChanged;

        public double MinValue
        {
            get
            {
                if (this.Element != null && this.Element.ValidationRules != null)
                {
                    foreach (var validationRule in this.Element.ValidationRules.OfType<DoubleValidationRule>())
                    {
                        return validationRule.MinValue;
                    }
                }
                return double.MinValue;
            }
        }

        protected virtual void OnMinValueChanged()
        {
            if (this.MinValueChanged != null)
            {
                this.MinValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MinValue");
        }

        public event EventHandler MinValueChanged;

        public double MaxValue
        {
            get
            {
                if (this.Element != null && this.Element.ValidationRules != null)
                {
                    foreach (var validationRule in this.Element.ValidationRules.OfType<DoubleValidationRule>())
                    {
                        return validationRule.MaxValue;
                    }
                }
                return double.MaxValue;
            }
        }

        protected virtual void OnMaxValueChanged()
        {
            if (this.MaxValueChanged != null)
            {
                this.MaxValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MaxValue");
        }

        public event EventHandler MaxValueChanged;

        public double Step
        {
            get
            {
                if (this.Element != null && this.Element.ValidationRules != null)
                {
                    foreach (var validationRule in this.Element.ValidationRules.OfType<DoubleValidationRule>())
                    {
                        return validationRule.Step;
                    }
                }
                return 0.1;
            }
        }

        protected virtual void OnStepChanged()
        {
            if (this.StepChanged != null)
            {
                this.StepChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Step");
        }

        public event EventHandler StepChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new DoubleConfigurationElement();
        }
    }
}
