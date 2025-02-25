using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class ComponentSettings : ViewModelBase
    {
        public static readonly DependencyProperty ConfigurationProperty = DependencyProperty.Register(
           "Configuration",
           typeof(IConfiguration),
           typeof(ComponentSettings),
           new PropertyMetadata(null, new PropertyChangedCallback(OnConfigurationChanged))
       );

        public static IConfiguration GetConfiguration(ComponentSettings source)
        {
            return (IConfiguration)source.GetValue(ConfigurationProperty);
        }

        public static void SetConfiguration(ComponentSettings source, IConfiguration value)
        {
            source.SetValue(ConfigurationProperty, value);
        }

        private static void OnConfigurationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var componentSettings = sender as ComponentSettings;
            if (componentSettings == null)
            {
                return;
            }
            componentSettings.OnConfigurationChanged();
        }

        public static readonly DependencyProperty SectionsProperty = DependencyProperty.Register(
           "Sections",
           typeof(StringCollection),
           typeof(ComponentSettings),
           new PropertyMetadata(new PropertyChangedCallback(OnSectionsChanged))
       );

        public static StringCollection GetSections(ComponentSettings source)
        {
            return (StringCollection)source.GetValue(SectionsProperty);
        }

        public static void SetSections(ComponentSettings source, StringCollection value)
        {
            source.SetValue(SectionsProperty, value);
        }

        public static void OnSectionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var componentSettings = sender as ComponentSettings;
            if (componentSettings == null)
            {
                return;
            }
            componentSettings.OnSectionsChanged();
        }

        public ComponentSettings()
        {
            this.Pages = new ObservableCollection<ComponentSettingsPage>();
        }

        public IConfiguration Configuration
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
            if (this.ConfigurationChanged != null)
            {
                this.ConfigurationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Configuration");
        }

        public event EventHandler ConfigurationChanged;

        public StringCollection Sections
        {
            get
            {
                return this.GetValue(SectionsProperty) as StringCollection;
            }
            set
            {
                this.SetValue(SectionsProperty, value);
            }
        }

        protected virtual void OnSectionsChanged()
        {
            if (this.SectionsChanged != null)
            {
                this.SectionsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Sections");
        }

        public event EventHandler SectionsChanged;

        public ObservableCollection<ComponentSettingsPage> Pages { get; private set; }

        private ComponentSettingsPage _SelectedPage { get; set; }

        public ComponentSettingsPage SelectedPage
        {
            get
            {
                if (this._SelectedPage == null)
                {
                    return ComponentSettingsPage.Empty;
                }
                return this._SelectedPage;
            }
            set
            {
                if (object.ReferenceEquals(this._SelectedPage, value))
                {
                    return;
                }
                this._SelectedPage = value;
                this.OnSelectedPageChanged();
            }
        }

        protected virtual void OnSelectedPageChanged()
        {
            if (this.SelectedPageChanged != null)
            {
                this.SelectedPageChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedPage");
        }

        public event EventHandler SelectedPageChanged;

        private string _Filter { get; set; }

        public string Filter
        {
            get
            {
                return this._Filter;
            }
            set
            {
                if (string.Equals(this._Filter, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                this._Filter = value;
                this.OnFilterChanged();
            }
        }

        protected virtual void OnFilterChanged()
        {
            this.Refresh();
            if (this.FilterChanged != null)
            {
                this.FilterChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Filter");
        }

        public event EventHandler FilterChanged;

        public ICommand ResetAllCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(
                     () => this.Configuration.Reset()
                 );
            }
        }

        public ICommand ResetPageCommand
        {
            get
            {
                return new Command(
                    () => this.SelectedPage.Reset(),
                    () => this.SelectedPage != null
                );
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return new Command(
                    () => this.Configuration.Save()
                );
            }
        }

        public void Refresh()
        {
            this.Pages.Clear();
            foreach (var section in this.Configuration.Sections.OrderBy(section => section.Name))
            {
                if (section.Flags.HasFlag(ConfigurationSectionFlags.System))
                {
                    //System config should not be presented to the user.
                    continue;
                }
                if (this.Sections != null)
                {
                    if (!this.Sections.Contains(section.Id, StringComparer.OrdinalIgnoreCase))
                    {
                        //Does not match section filter.
                        continue;
                    }
                }
                var sectionMatches = this.MatchesFilter(section);
                var elements = default(IEnumerable<ConfigurationElement>);
                if (sectionMatches)
                {
                    elements = section.Elements.Values;
                }
                else if (!this.MatchesFilter(section.Elements.Values, out elements))
                {
                    continue;
                }
                var page = new ComponentSettingsPage(section.Name, elements);
                this.Pages.Add(page);
            }
            this.SelectedPage = this.Pages.FirstOrDefault();
        }

        public virtual bool MatchesFilter(global::FoxTunes.ConfigurationSection section)
        {
            if (string.IsNullOrEmpty(this.Filter))
            {
                return true;
            }
            var values = new[] { section.Name, section.Description };
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value) && value.Contains(this.Filter, true))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool MatchesFilter(IEnumerable<ConfigurationElement> inputElements, out IEnumerable<ConfigurationElement> outputElements)
        {
            return (outputElements = inputElements.Where(element => this.MatchesFilter(element))).Any();
        }

        public virtual bool MatchesFilter(global::FoxTunes.ConfigurationElement element)
        {
            if (string.IsNullOrEmpty(this.Filter))
            {
                return true;
            }
            var values = new[] { element.Name, element.Description, element.Path };
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value) && value.Contains(this.Filter, true))
                {
                    return true;
                }
            }
            return false;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Settings();
        }
    }
}
