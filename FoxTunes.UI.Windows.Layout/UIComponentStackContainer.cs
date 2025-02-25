using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    [UIComponent("3451DAA4-C643-4CB2-8105-B441F0277559", children: UIComponentAttribute.UNLIMITED_CHILDREN, role: UIComponentRole.Container)]
    public class UIComponentStackContainer : UIComponentPanel
    {
        const string ADD = "AAAA";

        const string REMOVE = "BBBB";

        const string MOVE_UP = "CCCC";

        const string MOVE_DOWN = "DDDD";

        const string FILL = "EEEE";

        const string ALIGN_LEFT = "FFFF";

        const string ALIGN_RIGHT = "GGGG";

        const string ALIGN_TOP = "HHHH";

        const string ALIGN_BOTTOM = "IIII";

        new public const string HorizontalAlignment = nameof(global::System.Windows.HorizontalAlignment);

        new public const string VerticalAlignment = nameof(global::System.Windows.VerticalAlignment);

        public static readonly string Fill = Enum.GetName(typeof(global::System.Windows.HorizontalAlignment), global::System.Windows.HorizontalAlignment.Stretch);

        public static readonly string AlignLeft = Enum.GetName(typeof(global::System.Windows.HorizontalAlignment), global::System.Windows.HorizontalAlignment.Left);

        public static readonly string AlignRight = Enum.GetName(typeof(global::System.Windows.HorizontalAlignment), global::System.Windows.HorizontalAlignment.Right);

        public static readonly string AlignTop = Enum.GetName(typeof(global::System.Windows.VerticalAlignment), global::System.Windows.VerticalAlignment.Top);

        public static readonly string AlignBottom = Enum.GetName(typeof(global::System.Windows.VerticalAlignment), global::System.Windows.VerticalAlignment.Bottom);

        public UIComponentStackContainer()
        {
            this.EventHandlers = new Dictionary<UIComponentContainer, RoutedPropertyChangedEventHandler<UIComponentConfiguration>>();
            this.Grid = new Grid();
            this.Content = this.Grid;
        }

        public IDictionary<UIComponentContainer, RoutedPropertyChangedEventHandler<UIComponentConfiguration>> EventHandlers { get; private set; }

        public Grid Grid { get; private set; }

        protected override void OnConfigurationChanged()
        {
            this.UpdateChildren();
            base.OnConfigurationChanged();
        }

        protected virtual void UpdateChildren()
        {
            this.Grid.Children.Clear(UIDisposerFlags.Default);
            this.Grid.ColumnDefinitions.Clear();
            if (this.Configuration.Children.Count > 0)
            {
                foreach (var component in this.Configuration.Children)
                {
                    var horizontalAlignment = default(string);
                    var verticalAlignment = default(string);
                    if (component == null || !component.MetaData.TryGetValue(HorizontalAlignment, out horizontalAlignment))
                    {
                        horizontalAlignment = Fill;
                    }
                    if (component == null || !component.MetaData.TryGetValue(VerticalAlignment, out verticalAlignment))
                    {
                        verticalAlignment = Fill;
                    }
                    this.AddComponent(component, horizontalAlignment, verticalAlignment);
                }
            }
            else
            {
                var component = new UIComponentConfiguration();
                this.AddComponent(component, Fill, Fill);
                this.Configuration.Children.Add(component);
            }
        }

        protected virtual void AddComponent(UIComponentConfiguration component, string horizontalAlignment, string verticalAlignment)
        {
            var container = new UIComponentContainer()
            {
                Configuration = component,
                HorizontalAlignment = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), horizontalAlignment),
                VerticalAlignment = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), verticalAlignment),
            };
            var eventHandler = new RoutedPropertyChangedEventHandler<UIComponentConfiguration>((sender, e) =>
            {
                this.UpdateComponent(component, container.Configuration);
            });
            container.ConfigurationChanged += eventHandler;
            this.EventHandlers.Add(container, eventHandler);
            this.Grid.Children.Add(container);
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
                var horizontalAlignment = default(string);
                var verticalAlignment = default(string);
                var container = UIComponentDesignerOverlay.Container;

                if (container != null)
                {
                    if (!container.Configuration.MetaData.TryGetValue(HorizontalAlignment, out horizontalAlignment))
                    {
                        horizontalAlignment = Fill;
                    }
                    if (!container.Configuration.MetaData.TryGetValue(VerticalAlignment, out verticalAlignment))
                    {
                        verticalAlignment = Fill;
                    }
                }
                else
                {
                    //Don't know.
                }

                var isFill = string.Equals(horizontalAlignment, Fill, StringComparison.OrdinalIgnoreCase) && string.Equals(verticalAlignment, Fill, StringComparison.OrdinalIgnoreCase);

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
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    MOVE_UP,
                    Strings.UIComponentStackContainer_MoveUp,
                    attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    MOVE_DOWN,
                    Strings.UIComponentStackContainer_MoveDown
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    FILL,
                    Strings.UIComponentStackContainer_Fill,
                    attributes: (byte)((isFill ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    ALIGN_LEFT,
                    Strings.UIComponentStackContainer_AlignLeft,
                    attributes: (byte)((string.Equals(horizontalAlignment, AlignLeft, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    ALIGN_RIGHT,
                    Strings.UIComponentStackContainer_AlignRight,
                    attributes: string.Equals(horizontalAlignment, AlignRight, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    ALIGN_TOP,
                    Strings.UIComponentStackContainer_AlignTop,
                    attributes: (byte)((string.Equals(verticalAlignment, AlignTop, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    ALIGN_BOTTOM,
                    Strings.UIComponentStackContainer_AlignBottom,
                    attributes: string.Equals(verticalAlignment, AlignBottom, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
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
                    case MOVE_UP:
                        return this.MoveUp(container);
                    case MOVE_DOWN:
                        return this.MoveDown(container);
                    case FILL:
                        return this.SetFill(container);
                    case ALIGN_LEFT:
                        return this.SetHorizontalAlignment(container, AlignLeft);
                    case ALIGN_RIGHT:
                        return this.SetHorizontalAlignment(container, AlignRight);
                    case ALIGN_TOP:
                        return this.SetVerticalAlignment(container, AlignTop);
                    case ALIGN_BOTTOM:
                        return this.SetVerticalAlignment(container, AlignBottom);
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

        public Task MoveUp(UIComponentContainer container)
        {
            return Windows.Invoke(() =>
            {
                for (var a = 0; a < this.Configuration.Children.Count; a++)
                {
                    if (!object.ReferenceEquals(this.Configuration.Children[a], container.Configuration))
                    {
                        continue;
                    }
                    if (this.Configuration.Children.Count - 1 > a)
                    {
                        this.Configuration.Children[a] = this.Configuration.Children[a + 1];
                        this.Configuration.Children[a + 1] = container.Configuration;
                        this.UpdateChildren();
                    }
                    return;
                }
                //TODO: Component was not found.
                throw new NotImplementedException();
            });
        }

        public Task MoveDown(UIComponentContainer container)
        {
            return Windows.Invoke(() =>
            {
                for (var a = 0; a < this.Configuration.Children.Count; a++)
                {
                    if (!object.ReferenceEquals(this.Configuration.Children[a], container.Configuration))
                    {
                        continue;
                    }
                    if (a > 0)
                    {
                        this.Configuration.Children[a] = this.Configuration.Children[a - 1];
                        this.Configuration.Children[a - 1] = container.Configuration;
                        this.UpdateChildren();
                    }
                    return;
                }
                //TODO: Component was not found.
                throw new NotImplementedException();
            });
        }

        public Task SetFill(UIComponentContainer container)
        {
            return Windows.Invoke(() =>
            {
                container.Configuration.MetaData.AddOrUpdate(HorizontalAlignment, Fill);
                container.Configuration.MetaData.AddOrUpdate(VerticalAlignment, Fill);
                this.UpdateChildren();
            });
        }

        public Task SetHorizontalAlignment(UIComponentContainer container, string alignment)
        {
            return Windows.Invoke(() =>
            {
                container.Configuration.MetaData.AddOrUpdate(HorizontalAlignment, alignment);
                this.UpdateChildren();
            });
        }

        public Task SetVerticalAlignment(UIComponentContainer container, string alignment)
        {
            return Windows.Invoke(() =>
            {
                container.Configuration.MetaData.AddOrUpdate(VerticalAlignment, alignment);
                this.UpdateChildren();
            });
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
