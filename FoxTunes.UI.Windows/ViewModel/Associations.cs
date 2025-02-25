using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Associations : ViewModelBase
    {
        public bool Supported
        {
            get
            {
                return Publication.IsPortable;
            }
        }

        public IOutput Output { get; private set; }

        public ObservableCollection<Association> FileAssociations { get; private set; }

        public IEnumerable<IFileAssociation> Enabled
        {
            get
            {
                return this.FileAssociations
                    .Where(association => association.FileAssociation != null && association.IsSelected)
                    .Select(association => association.FileAssociation);
            }
        }

        public IEnumerable<IFileAssociation> Disabled
        {
            get
            {
                return this.FileAssociations
                    .Where(association => association.FileAssociation != null && !association.IsSelected)
                    .Select(association => association.FileAssociation);
            }
        }

        protected override void InitializeComponent(ICore core)
        {
            this.FileAssociations = new ObservableCollection<Association>();
            this.Output = core.Components.Output;
            this.Refresh();
            base.InitializeComponent(core);
        }

        protected virtual void Refresh()
        {
            if (!this.Supported)
            {
                return;
            }
            var fileAssociations = ComponentRegistry.Instance.GetComponent<IFileAssociations>();
            if (fileAssociations == null)
            {
                return;
            }
            var extensions = fileAssociations.Associations.Select(
                association => association.Extension.TrimStart('.')
            ).ToArray();
            foreach (var association in this.FileAssociations)
            {
                association.Dispose();
            }
            this.FileAssociations.Clear();
            this.FileAssociations.AddRange(
                this.Output.SupportedExtensions
                    .OrderBy(extension => extension)
                    .Select(extension => new Association(
                        fileAssociations.Create(extension),
                        extensions.Contains(extension)
                    )
                )
            );
        }

        public ICommand SelectAllCommand
        {
            get
            {
                return new Command(() => this.SelectAll(true), () => this.Supported);
            }
        }

        public ICommand SelectNoneCommand
        {
            get
            {
                return new Command(() => this.SelectAll(false), () => this.Supported);
            }
        }

        public void SelectAll(bool selected)
        {
            foreach (var association in this.FileAssociations)
            {
                association.IsSelected = selected;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return new Command(this.Save, () => this.Supported);
            }
        }

        public void Save()
        {
            var fileAssociations = ComponentRegistry.Instance.GetComponent<IFileAssociations>();
            if (fileAssociations == null)
            {
                return;
            }
            if (this.Enabled.Any())
            {
                fileAssociations.Enable();
            }
            else
            {
                fileAssociations.Disable();
            }
            fileAssociations.Disable(this.Disabled);
            fileAssociations.Enable(this.Enabled);
            this.Refresh();
        }

        public ICommand CancelCommand
        {
            get
            {
                return new Command(this.Cancel, () => this.Supported);
            }
        }

        public void Cancel()
        {
            this.Refresh();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Associations();
        }
    }

    public class Association : ViewModelBase
    {
        public Association(IFileAssociation fileAssociation, bool isSelected)
        {
            this.FileAssociation = fileAssociation;
            this.IsSelected = isSelected;
        }

        public IFileAssociation FileAssociation { get; private set; }

        private bool _IsSelected { get; set; }

        public bool IsSelected
        {
            get
            {
                return this._IsSelected;
            }
            set
            {
                this._IsSelected = value;
                this.OnIsSelectedChanged();
            }
        }

        protected virtual void OnIsSelectedChanged()
        {
            if (this.IsSelectedChanged != null)
            {
                this.IsSelectedChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsSelected");
        }

        public event EventHandler IsSelectedChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new Association(null, false);
        }
    }
}
