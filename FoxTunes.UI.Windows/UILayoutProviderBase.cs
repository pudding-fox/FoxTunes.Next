using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public abstract class UILayoutProviderBase : StandardComponent, IUILayoutProvider
    {
        public abstract string Id { get; }

        public abstract string Name { get; }

        public abstract string Description { get; }

        public bool Active
        {
            get
            {
                return object.ReferenceEquals(LayoutManager.Instance.Provider, this);
            }
        }

        public abstract UIComponentBase Load(UILayoutTemplate template);

        protected virtual void OnUpdated()
        {
            if (this.Updated == null)
            {
                return;
            }
            this.Updated(this, EventArgs.Empty);
        }

        public event EventHandler Updated;

        public virtual UIComponentBase PresetSelector
        {
            get
            {
                return null;
            }
        }
    }
}
