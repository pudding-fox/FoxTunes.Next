using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class Wrapper : ViewModelBase
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(object),
            typeof(Wrapper),
            new PropertyMetadata(new PropertyChangedCallback(OnValueChanged))
        );

        public static object GetValue(Wrapper source)
        {
            return source.GetValue(ValueProperty);
        }

        public static void SetValue(Wrapper source, object value)
        {
            source.SetValue(ValueProperty, value);
        }

        public static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var wrapper = sender as Wrapper;
            if (wrapper == null)
            {
                return;
            }
            wrapper.OnValueChanged();
        }

        public object Value
        {
            get
            {
                return this.GetValue(ValueProperty);
            }
            set
            {
                this.SetValue(ValueProperty, value);
            }
        }

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Value");
        }

        public event EventHandler ValueChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new Wrapper();
        }
    }

    public class Wrapper<T> : ViewModelBase where T : class
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(T),
            typeof(Wrapper<T>),
            new PropertyMetadata(new PropertyChangedCallback(OnValueChanged))
        );

        public static object GetValue(Wrapper<T> source)
        {
            return source.GetValue(ValueProperty);
        }

        public static void SetValue(Wrapper<T> source, T value)
        {
            source.SetValue(ValueProperty, value);
        }

        public static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var wrapper = sender as Wrapper<T>;
            if (wrapper == null)
            {
                return;
            }
            wrapper.OnValueChanged();
        }

        public T Value
        {
            get
            {
                return this.GetValue(ValueProperty) as T;
            }
            set
            {
                this.SetValue(ValueProperty, value);
            }
        }

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Value");
        }

        public event EventHandler ValueChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new Wrapper<T>();
        }
    }
}
