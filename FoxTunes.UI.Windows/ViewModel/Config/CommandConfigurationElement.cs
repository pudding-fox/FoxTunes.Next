using System;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel.Config
{
    public class CommandConfigurationElement : ViewModelBase
    {
        public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(
           "Element",
           typeof(global::FoxTunes.CommandConfigurationElement),
           typeof(CommandConfigurationElement),
           new PropertyMetadata(new PropertyChangedCallback(OnElementChanged))
       );

        public static global::FoxTunes.CommandConfigurationElement GetElement(ViewModelBase source)
        {
            return (global::FoxTunes.CommandConfigurationElement)source.GetValue(ElementProperty);
        }

        public static void SetElement(ViewModelBase source, global::FoxTunes.CommandConfigurationElement value)
        {
            source.SetValue(ElementProperty, value);
        }

        public static void OnElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = sender as CommandConfigurationElement;
            if (viewModel == null)
            {
                return;
            }
            viewModel.OnElementChanged();
        }

        public global::FoxTunes.CommandConfigurationElement Element
        {
            get
            {
                return this.GetValue(ElementProperty) as global::FoxTunes.CommandConfigurationElement;
            }
            set
            {
                this.SetValue(ElementProperty, value);
            }
        }

        protected virtual void OnElementChanged()
        {
            this.OnInvokeCommandChanged();
            if (this.ElementChanged != null)
            {
                this.ElementChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Element");
        }

        public event EventHandler ElementChanged;

        public ICommand InvokeCommand
        {
            get
            {
                if (this.Element == null)
                {
                    return null;
                }
                return CommandFactory.Instance.CreateCommand(
                    new Action(this.Element.Invoke)
                );
            }
        }

        protected virtual void OnInvokeCommandChanged()
        {
            if (this.InvokeCommandChanged != null)
            {
                this.InvokeCommandChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("InvokeCommand");
        }

        public event EventHandler InvokeCommandChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new CommandConfigurationElement();
        }
    }
}
