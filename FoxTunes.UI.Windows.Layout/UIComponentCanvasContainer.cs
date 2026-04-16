using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    [UIComponent("70AFD32E-FFE3-4800-BE55-484F7AA4F603", children: UIComponentAttribute.UNLIMITED_CHILDREN, role: UIComponentRole.Container)]
    public class UIComponentCanvasContainer : UIComponentPanel
    {
        const string ADD = "AAAA";

        const string REMOVE = "BBBB";

        public const string Top = "Top";

        public const string Left = "Left";

        new public const string Width = nameof(Width);

        new public const string Height = nameof(Height);

        public UIComponentCanvasContainer()
        {
            this.EventHandlers = new Dictionary<UIComponentContainer, RoutedPropertyChangedEventHandler<UIComponentConfiguration>>();
            this.Canvas = new Canvas();
            this.Content = this.Canvas;
        }

        public IDictionary<UIComponentContainer, RoutedPropertyChangedEventHandler<UIComponentConfiguration>> EventHandlers { get; private set; }

        public Canvas Canvas { get; private set; }

        protected override void OnConfigurationChanged()
        {
            this.UpdateChildren();
            base.OnConfigurationChanged();
        }

        protected virtual void UpdateChildren()
        {
            this.Canvas.Children.Clear(UIDisposerFlags.Default);
            if (this.Configuration.Children.Count > 0)
            {
                foreach (var component in this.Configuration.Children)
                {
                    var top = default(string);
                    var left = default(string);
                    var width = default(string);
                    var height = default(string);
                    if (component == null || !component.MetaData.TryGetValue(Top, out top))
                    {
                        top = "0";
                    }
                    if (component == null || !component.MetaData.TryGetValue(Left, out left))
                    {
                        left = "0";
                    }
                    if (component == null || !component.MetaData.TryGetValue(Width, out width))
                    {
                        width = string.Empty;
                    }
                    if (component == null || !component.MetaData.TryGetValue(Height, out height))
                    {
                        height = string.Empty;
                    }
                    this.AddComponent(component, top, left, width, height);
                }
            }
            else
            {
                {
                    var top = "0";
                    var left = "0";
                    var width = string.Empty;
                    var height = string.Empty;
                    var component = new UIComponentConfiguration();
                    this.AddComponent(component, top, left, width, height);
                    this.Configuration.Children.Add(component);
                }
            }
        }

        protected virtual void AddComponent(UIComponentConfiguration component, string top, string left, string width, string height)
        {
            var container = new UIComponentContainer()
            {
                Configuration = component
            };
            container.SetValue(Canvas.TopProperty, Convert.ToDouble(top));
            container.SetValue(Canvas.LeftProperty, Convert.ToDouble(left));
            if (!string.IsNullOrEmpty(width))
            {
                container.Width = Convert.ToDouble(width);
            }
            if (!string.IsNullOrEmpty(height))
            {
                container.Height = Convert.ToDouble(height);
            }
            var eventHandler = new RoutedPropertyChangedEventHandler<UIComponentConfiguration>((sender, e) =>
            {
                this.UpdateComponent(component, container.Configuration);
            });
            container.ConfigurationChanged += eventHandler;
            this.EventHandlers.Add(container, eventHandler);
            this.Canvas.Children.Add(container);
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
                    ADD,
                    Strings.UIComponentStackContainer_Add
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    REMOVE,
                    Strings.UIComponentStackContainer_Remove
                );
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case ADD:
                    return this.Add();
            }
            if (component.Source is UIComponentContainer container)
            {
                switch (component.Id)
                {
                    case REMOVE:
                        return this.Remove(container);
                }
            }
            return base.InvokeAsync(component);
        }


        public Task Add()
        {
            return Windows.Invoke(() =>
            {
                this.Configuration.Children.Add(new UIComponentConfiguration());
                this.UpdateChildren();
            });
        }

        public Task Remove(UIComponentContainer container)
        {
            return Windows.Invoke(() =>
            {
                for (var a = 0; a < this.Configuration.Children.Count; a++)
                {
                    if (!object.ReferenceEquals(this.Configuration.Children[a], container.Configuration))
                    {
                        continue;
                    }
                    this.Configuration.Children.RemoveAt(a);
                    this.UpdateChildren();
                    return;
                }
                //TODO: Component was not found.
                throw new NotImplementedException();
            });
        }
    }
}
