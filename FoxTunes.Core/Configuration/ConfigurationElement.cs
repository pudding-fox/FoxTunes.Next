using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace FoxTunes
{
    public abstract class ConfigurationElement : INotifyPropertyChanged
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        private ConfigurationElement()
        {
            this.IsHidden = false;
            this.Flags = ConfigurationElementFlags.None;
        }

        protected ConfigurationElement(string id, string name = null, string description = null, string path = null)
            : this()
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.Path = path;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public string Path { get; private set; }

        public bool IsVisible
        {
            get
            {
                return !this.IsHidden;
            }
        }

        private bool _IsHidden { get; set; }

        public bool IsHidden
        {
            get
            {
                return this._IsHidden;
            }
            set
            {
                this._IsHidden = value;
                this.OnIsHiddenChanged();
            }
        }

        protected virtual void OnIsHiddenChanged()
        {
            if (this.IsHiddenChanged != null)
            {
                this.IsHiddenChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsHidden");
        }

        public event EventHandler IsHiddenChanged;

        public IList<ValidationRule> ValidationRules { get; private set; }

        public bool IsInitialized { get; protected set; }

        public ConfigurationElement WithValidationRule(ValidationRule validationRule)
        {
            if (this.ValidationRules == null)
            {
                this.ValidationRules = new List<ValidationRule>();
            }
            this.ValidationRules.Add(validationRule);
            return this;
        }

        private ConfigurationElementFlags _Flags { get; set; }

        public ConfigurationElementFlags Flags
        {
            get
            {
                return this._Flags;
            }
            set
            {
                this._Flags = value;
                this.OnFlagsChanged();
            }
        }

        protected virtual void OnFlagsChanged()
        {
            if (this.FlagsChanged != null)
            {
                this.FlagsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Flags");
        }

        public event EventHandler FlagsChanged;

        public ConfigurationElement WithFlags(ConfigurationElementFlags flags)
        {
            this.Flags = flags;
            return this;
        }

        private IList<Dependency> _Dependencies;

        public IList<Dependency> Dependencies
        {
            get
            {
                return this._Dependencies;
            }
            private set
            {
                this._Dependencies = value;
                this.OnDependenciesChanged();
            }
        }

        protected virtual void OnDependenciesChanged()
        {
            if (this.DependenciesChanged != null)
            {
                this.DependenciesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Dependencies");
        }

        public event EventHandler DependenciesChanged;

        public virtual void InitializeComponent()
        {
            this.IsInitialized = true;
        }

        public virtual void Update(ConfigurationElement element)
        {
            if (string.IsNullOrEmpty(this.Name) && !string.IsNullOrEmpty(element.Name))
            {
                this.Name = element.Name;
            }
            if (string.IsNullOrEmpty(this.Description) && !string.IsNullOrEmpty(element.Description))
            {
                this.Description = element.Description;
            }
            if (string.IsNullOrEmpty(this.Path) && !string.IsNullOrEmpty(element.Path))
            {
                this.Path = element.Path;
            }
            if (element.ValidationRules != null && element.ValidationRules.Count > 0)
            {
                if (this.ValidationRules == null)
                {
                    this.ValidationRules = new List<ValidationRule>(element.ValidationRules);
                }
                else
                {
                    this.ValidationRules.AddRange(element.ValidationRules);
                }
            }
            this.Flags |= element.Flags;
            if (element.Dependencies != null && element.Dependencies.Count > 0)
            {
                if (this.Dependencies == null)
                {
                    this.Dependencies = new List<Dependency>(element.Dependencies);
                }
                else
                {
                    this.Dependencies.AddRange(element.Dependencies);
                }
            }
        }

        public ConfigurationElement Hide()
        {
            this.IsHidden = true;
            return this;
        }

        public ConfigurationElement Show()
        {
            this.IsHidden = false;
            return this;
        }

        public abstract bool IsModified { get; }

        public ConfigurationElement DependsOn(string sectionId, string elementId, bool negate = false)
        {
            return this.DependsOn(new BooleanDependency(sectionId, elementId, negate));
        }

        public ConfigurationElement DependsOn(string sectionId, string elementId, string optionId, bool negate = false)
        {
            return this.DependsOn(new SelectionDependency(sectionId, elementId, optionId, negate));
        }

        public ConfigurationElement DependsOn(Dependency dependency)
        {
            if (this.Dependencies == null)
            {
                this.Dependencies = new List<Dependency>();
            }
            this.Dependencies.Add(dependency);
            return this;
        }

        public void ConnectDependencies(IConfiguration configuration)
        {
            if (this.Dependencies == null)
            {
                return;
            }
            var dependencies = this.Dependencies.ToDictionary(
                dependency => dependency,
                dependency => configuration.GetElement(dependency.SectionId, dependency.ElementId)
            );
            var handler = new EventHandler((sender, e) =>
            {
                if (dependencies.All(pair => pair.Key.Validate(pair.Value)))
                {
                    this.Show();
                }
                else
                {
                    this.Hide();
                }
            });
            foreach (var pair in dependencies)
            {
                pair.Key.AddHandler(pair.Value, handler);
            }
            handler(this, EventArgs.Empty);
        }

        public abstract void Reset();

        public abstract string GetPersistentValue();

        public abstract void SetPersistentValue(string value);

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

    public abstract class ConfigurationElement<T> : ConfigurationElement
    {
        protected ConfigurationElement(string id, string name = null, string description = null, string path = null) : base(id, name, description, path)
        {

        }

        public T DefaultValue { get; private set; }

        private T _Value { get; set; }

        public virtual T Value
        {
            get
            {
                return this._Value;
            }
            set
            {
                if (EqualityComparer<T>.Default.Equals(this.Value, value))
                {
                    return;
                }
                this.SetValue(value);
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

        public void SetValue(T value)
        {
            this._Value = value;
            this.OnValueChanged();
        }

        public ConfigurationElement<T> WithValue(T value)
        {
            this.Value = value;
            return this;
        }

        public ConfigurationElement<T> ConnectValue(Action<T> action)
        {
            var handler = new EventHandler((sender, e) =>
            {
                try
                {
                    action(this.Value);
                }
                catch (Exception exception)
                {
                    Logger.Write(
                        typeof(ConfigurationElement),
                        LogLevel.Warn,
                        "Failed to connect configuration value \"{0}\" = \"{1}\": {2}",
                        this.Id,
                        Convert.ToString(this.Value),
                        exception.Message
                    );
                }
            });
            handler(this, EventArgs.Empty);
            this.ValueChanged += handler;
            return this;
        }

        public override void InitializeComponent()
        {
            //TODO: This log message is noisy.
            //Logger.Write(typeof(ConfigurationSection), LogLevel.Trace, "Setting default value for configuration element \"{0}\": {1}", this.Name, Convert.ToString(this.Value));
            this.DefaultValue = this.Value;
            base.InitializeComponent();
        }

        public override void Update(ConfigurationElement element)
        {
            if (element is ConfigurationElement<T>)
            {
                this.Update(element as ConfigurationElement<T>);
            }
            base.Update(element);
        }

        protected virtual void Update(ConfigurationElement<T> element)
        {
            if (EqualityComparer<T>.Default.Equals(element.Value, default(T)))
            {
                return;
            }
            this.Value = element.Value;
        }

        public override void Reset()
        {
            this.Value = this.DefaultValue;
        }

        public override bool IsModified
        {
            get
            {
                if (EqualityComparer<T>.Default.Equals(this.Value, this.DefaultValue))
                {
                    return false;
                }
                return true;
            }
        }

        public override string GetPersistentValue()
        {
            return Convert.ToString(this.Value);
        }

        public override void SetPersistentValue(string value)
        {
            this.Value = (T)Convert.ChangeType(value, typeof(T));
        }
    }

    [Flags]
    public enum ConfigurationElementFlags : byte
    {
        None = 0,
        MultiLine = 1,
        FileName = 2,
        FolderName = 4,
        Secret = 8,
        Script = 16,
        FontFamily = 32
    }
}
