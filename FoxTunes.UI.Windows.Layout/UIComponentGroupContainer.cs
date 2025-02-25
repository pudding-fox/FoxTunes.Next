using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes
{
    [UIComponent("D5407817-6EBB-4D79-870D-664CBEB2D069", children: UIComponentAttribute.ONE_CHILD, role: UIComponentRole.Container)]
    public class UIComponentGroupContainer : UIComponentPanel
    {
        const string RENAME = "AAAA";

        public const string Header = nameof(global::System.Windows.Controls.GroupBox.Header);

        public UIComponentGroupContainer()
        {
            this.EventHandlers = new Dictionary<UIComponentContainer, RoutedPropertyChangedEventHandler<UIComponentConfiguration>>();
            this.GroupBox = new GroupBox();
            this.Content = this.GroupBox;
            this.CreateBindings();
        }

        public IDictionary<UIComponentContainer, RoutedPropertyChangedEventHandler<UIComponentConfiguration>> EventHandlers { get; private set; }

        public GroupBox GroupBox { get; private set; }

        new protected virtual void CreateBindings()
        {
            this.SetBinding(
                IsComponentEnabledProperty,
                new Binding()
                {
                    Source = this.GroupBox,
                    Path = new PropertyPath("Content.Content.IsComponentEnabled"),
                    FallbackValue = true
                }
            );
        }

        protected override void OnConfigurationChanged()
        {
            this.UpdateHeader();
            this.UpdateChildren();
            base.OnConfigurationChanged();
        }

        protected virtual void UpdateHeader()
        {
            this.GroupBox.Header = this.GetHeader();
        }

        protected virtual void UpdateChildren()
        {
            if (this.GroupBox.Content is FrameworkElement frameworkElement)
            {
                UIDisposer.Dispose(frameworkElement);
            }
            if (this.Configuration.Children.Count > 0)
            {
                foreach (var component in this.Configuration.Children)
                {
                    this.AddComponent(component);
                }
            }
            else
            {
                var component = new UIComponentConfiguration();
                this.AddComponent(component);
                this.Configuration.Children.Add(component);
            }
        }

        protected virtual void AddComponent(UIComponentConfiguration component)
        {
            var container = new UIComponentContainer()
            {
                Configuration = component
            };
            var eventHandler = new RoutedPropertyChangedEventHandler<UIComponentConfiguration>((sender, e) =>
            {
                this.UpdateComponent(component, container.Configuration);
            });
            container.ConfigurationChanged += eventHandler;
            this.EventHandlers.Add(container, eventHandler);
            this.GroupBox.Content = container;
        }

        protected virtual void UpdateComponent(UIComponentConfiguration originalComponent, UIComponentConfiguration newComponent)
        {
            for (var a = 0; a < this.Configuration.Children.Count; a++)
            {
                if (!object.ReferenceEquals(this.Configuration.Children[a], originalComponent))
                {
                    continue;
                }
                this.Configuration.Children[a] = newComponent;
                this.UpdateChildren();
                return;
            }
            //TODO: Component was not found.
            throw new NotImplementedException();
        }

        protected virtual void OnComponentChanged(object sender, EventArgs e)
        {
            var container = sender as UIComponentContainer;
            if (container == null)
            {
                return;
            }
        }

        public override IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_GLOBAL;
            }
        }

        public override IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    RENAME,
                    Strings.UIComponentTabContainer_Rename,
                    attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                );
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case RENAME:
                    return this.Rename();
            }
            return base.InvokeAsync(component);
        }

        public Task Rename()
        {
            return Windows.Invoke(() =>
            {
                var header = InputBox.ShowDialog(Strings.UIComponentGroupContainer_Rename, this.GetHeader());
                if (this.Configuration == null)
                {
                    this.Configuration = new UIComponentConfiguration();
                }
                if (!string.IsNullOrEmpty(header))
                {
                    this.Configuration.MetaData.AddOrUpdate(Header, header);
                }
                else
                {
                    this.Configuration.MetaData.TryRemove(Header);
                }
                this.UpdateHeader();
            });
        }

        public string GetHeader()
        {
            var header = default(string);
            if (this.Configuration.MetaData.TryGetValue(Header, out header) && !string.IsNullOrEmpty(header))
            {
                return header;
            }
            if (!this.Configuration.Component.IsEmpty)
            {
                return this.Configuration.Component.Name;
            }
            return Strings.UIComponentGroupContainer_Name;
        }

        protected override void OnDisposing()
        {
            if (this.EventHandlers != null)
            {
                foreach (var pair in this.EventHandlers)
                {
                    pair.Key.ConfigurationChanged -= pair.Value;
                }
            }
            base.OnDisposing();
        }
    }
}
