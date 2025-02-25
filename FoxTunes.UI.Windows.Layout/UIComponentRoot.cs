using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Windows.Data;

namespace FoxTunes
{
    public class UIComponentRoot : UIComponentPanel, IDisposable
    {
        static UIComponentRoot()
        {
            Instances = new List<WeakReference<UIComponentRoot>>();
        }

        private static IList<WeakReference<UIComponentRoot>> Instances { get; set; }

        public static IEnumerable<UIComponentRoot> Active
        {
            get
            {
                lock (Instances)
                {
                    return Instances
                        .Where(instance => instance != null && instance.IsAlive)
                        .Select(instance => instance.Target)
                        .ToArray();
                }
            }
        }

        protected static void OnActiveChanged(UIComponentRoot sender)
        {
            if (ActiveChanged == null)
            {
                return;
            }
            ActiveChanged(sender, EventArgs.Empty);
        }

        public static event EventHandler ActiveChanged;

        public UIComponentRoot()
        {
            var container = new UIComponentContainer();
            //TODO: Should we create this binding now or on Loaded?
            container.SetBinding(
                UIComponentContainer.ConfigurationProperty,
                new Binding()
                {
                    Source = this,
                    Path = new PropertyPath(nameof(this.Configuration))
                }
            );
            this.Content = container;
            lock (Instances)
            {
                Instances.Add(new WeakReference<UIComponentRoot>(this));
            }
            OnActiveChanged(this);
        }

        public UIComponentContainer Container
        {
            get
            {
                return this.Content as UIComponentContainer;
            }
        }

        protected override void CreateBindings()
        {
            //Nothing to do.
        }

        public bool Contains(UIComponentConfiguration configuration, out UIComponentContainer container)
        {
            foreach (var child in this.FindChildren<UIComponentContainer>())
            {
                if (!object.ReferenceEquals(child.Configuration, configuration))
                {
                    continue;
                }
                container = child;
                return true;
            }
            container = null;
            return false;
        }

        protected override void OnDisposing()
        {
            lock (Instances)
            {
                for (var a = Instances.Count - 1; a >= 0; a--)
                {
                    var instance = Instances[a];
                    if (instance == null || !instance.IsAlive)
                    {
                        Instances.RemoveAt(a);
                    }
                    else if (object.ReferenceEquals(this, instance.Target))
                    {
                        Instances.RemoveAt(a);
                    }
                }
            }
            OnActiveChanged(this);
            base.OnDisposing();
        }
    }
}
