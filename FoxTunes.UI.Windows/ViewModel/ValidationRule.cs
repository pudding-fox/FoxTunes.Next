using FoxTunes.Interfaces;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;

namespace FoxTunes.ViewModel
{
    public class ValidationRule : global::System.Windows.Controls.ValidationRule, INotifyPropertyChanged
    {
        private Wrapper _ConfigurationElement { get; set; }

        public Wrapper ConfigurationElement
        {
            get
            {
                return this._ConfigurationElement;
            }
            set
            {
                this._ConfigurationElement = value;
                this.OnConfigurationElementChanged();
            }
        }

        protected virtual void OnConfigurationElementChanged()
        {
            if (this.ConfigurationElementChanged != null)
            {
                this.ConfigurationElementChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ConfigurationElement");
        }

        public event EventHandler ConfigurationElementChanged;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var configurationElement = this.ConfigurationElement.Value as ConfigurationElement;
            if (configurationElement != null && configurationElement.ValidationRules != null)
            {
                foreach (var validationRule in configurationElement.ValidationRules)
                {
                    var message = default(string);
                    if (!validationRule.Validate(value, out message))
                    {
                        ComponentRegistry.Instance.GetComponent<IUserInterface>().Warn(message);
                        return new ValidationResult(false, message);
                    }
                }
            }
            return ValidationResult.ValidResult;
        }

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
