using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace FoxTunes
{
    public class UIComponentConfiguration
    {
        public UIComponentConfiguration()
        {
            this.Id = Guid.NewGuid();
            this.Component = UIComponent.None;
            this.Children = new List<UIComponentConfiguration>();
            this.MetaData = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public UIComponentConfiguration(UIComponent component, params UIComponentConfiguration[] children) : this()
        {
            this.Component = component;
            if (children.Length > 0)
            {
                this.Children.AddRange(children);
            }
        }

        public Guid Id { get; private set; }

        private UIComponent _Component { get; set; }

        public UIComponent Component
        {
            get
            {
                return this._Component;
            }
            set
            {
                this._Component = value;
                this.OnComponentChanged();
            }
        }

        protected virtual void OnComponentChanged()
        {
            if (this.ComponentChanged != null)
            {
                this.ComponentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Component");
        }

        public event EventHandler ComponentChanged;

        private IList<UIComponentConfiguration> _Children { get; set; }

        public IList<UIComponentConfiguration> Children
        {
            get
            {
                return this._Children;
            }
            set
            {
                this._Children = value;
                this.OnChildrenChanged();
            }
        }

        protected virtual void OnChildrenChanged()
        {
            if (this.ChildrenChanged != null)
            {
                this.ChildrenChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Children");
        }

        public event EventHandler ChildrenChanged;

        private ConcurrentDictionary<string, string> _MetaData { get; set; }

        public ConcurrentDictionary<string, string> MetaData
        {
            get
            {
                return this._MetaData;
            }
            set
            {
                this._MetaData = value;
                this.OnMetaDataChanged();
            }
        }

        protected virtual void OnMetaDataChanged()
        {
            if (this.MetaDataChanged != null)
            {
                this.MetaDataChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MetaData");
        }

        public event EventHandler MetaDataChanged;

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
