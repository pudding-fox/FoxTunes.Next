using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Plugins : ViewModelBase
    {
        public Plugins() : base(false)
        {
            this.Items = new ObservableCollection<Plugin>();
            this.InitializeComponent(Core.Instance);
        }

        public bool Supported
        {
            get
            {
                return ComponentResolver.Instance.Enabled;
            }
        }

        public ObservableCollection<Plugin> Items { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            this.Refresh();
            base.InitializeComponent(core);
        }

        protected virtual void Refresh()
        {
            if (!this.Supported)
            {
                return;
            }
            this.Items.Clear();
            this.Items.AddRange(GetItems());
        }

        public ICommand SaveCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Save, () => this.Supported);
            }
        }

        public void Save()
        {
            foreach (var plugin in this.Items)
            {
                if (plugin.Components.Count <= 1 || plugin.SelectedComponent == null)
                {
                    //Slot is not ambiguous.
                    continue;
                }
                ComponentResolver.Instance.Add(plugin.Id, plugin.SelectedComponent.Id);
                ComponentResolver.Instance.AddConflict(plugin.Id);
            }
            ComponentResolver.Instance.Save();
            this.UserInterface.Warn(string.Format(Strings.Plugins_RestartWarning, ComponentResolver.FileName.GetName()));
        }

        public ICommand CancelCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Cancel, () => this.Supported);
            }
        }

        public void Cancel()
        {
            this.Refresh();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Plugins();
        }

        private static IEnumerable<Plugin> GetItems()
        {
            var slots = ComponentScanner.Instance.GetComponentsBySlot();
            foreach (var pair in ComponentSlots.Lookup)
            {
                var types = default(IList<Type>);
                if (!slots.TryGetValue(pair.Value, out types))
                {
                    //Nothing to configure.
                    continue;
                }
                var plugin = new Plugin(pair.Value, pair.Key, types);
                yield return plugin;
            }
        }
    }

    public class Plugin : ViewModelBase
    {
        public Plugin() : base(false)
        {

        }

        public Plugin(string id, string name, IEnumerable<Type> types) : this()
        {
            this.Id = id;
            this.Name = name;
            this.Types = types;
            this.InitializeComponent(Core.Instance);
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public IEnumerable<Type> Types { get; private set; }

        public ObservableCollection<PluginComponent> Components { get; private set; }

        private PluginComponent _SelectedComponent { get; set; }

        public PluginComponent SelectedComponent
        {
            get
            {
                return this._SelectedComponent;
            }
            set
            {
                this._SelectedComponent = value;
                this.OnSelectedComponentChanged();
            }
        }

        protected virtual void OnSelectedComponentChanged()
        {
            if (this.SelectedComponentChanged != null)
            {
                this.SelectedComponentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedComponent");
        }

        public event EventHandler SelectedComponentChanged;

        protected override void InitializeComponent(ICore core)
        {
            this.Components = new ObservableCollection<PluginComponent>(GetComponents(this.Types));
            var id = default(string);
            if (ComponentResolver.Instance.Get(this.Id, out id))
            {
                this.SelectedComponent = this.Components.FirstOrDefault(component => string.Equals(component.Id, id, StringComparison.OrdinalIgnoreCase));
            }
            if (this.SelectedComponent == null)
            {
                this.SelectedComponent = this.Components.FirstOrDefault();
            }
            base.InitializeComponent(core);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Plugin();
        }

        private static IEnumerable<PluginComponent> GetComponents(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                var component = new PluginComponent(type);

                yield return component;
            }
        }
    }

    public class PluginComponent : ViewModelBase
    {
        public PluginComponent()
        {

        }

        public PluginComponent(Type type) : this()
        {
            var component = default(ComponentAttribute);
            if (type.HasCustomAttribute<ComponentAttribute>(out component))
            {
                this.Id = component.Id;
                if (!string.IsNullOrEmpty(component.Name))
                {
                    this.Name = component.Name;
                }
                else
                {
                    this.Name = type.AssemblyQualifiedName;
                }
                if (!string.IsNullOrEmpty(component.Description))
                {
                    this.Description = component.Description;
                }
                else
                {
                    this.Description = Strings.Plugins_NoDescription;
                }
            }
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        protected override Freezable CreateInstanceCore()
        {
            return new PluginComponent();
        }
    }
}
