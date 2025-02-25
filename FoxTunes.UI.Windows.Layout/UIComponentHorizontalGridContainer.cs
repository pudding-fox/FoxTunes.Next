using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for UIComponentHorizontalGridContainer.xaml
    /// </summary>
    [UIComponent("C1BD2AF7-ACF8-4710-99A3-AB5F34C46A90", children: UIComponentAttribute.UNLIMITED_CHILDREN, role: UIComponentRole.Container)]
    public partial class UIComponentHorizontalGridContainer : UIComponentGridContainer
    {
        const string ADD = "AAAA";

        const string REMOVE = "BBBB";

        const string MOVE_LEFT = "CCCC";

        const string MOVE_RIGHT = "DDDD";

        const string ALIGN_LEFT = "EEEE";

        const string ALIGN_STRETCH = "FFFF";

        const string ALIGN_RIGHT = "GGGG";

        public const string Alignment = "Alignment";

        public const string AlignLeft = "Left";

        public const string AlignStretch = "Stretch";

        public const string AlignRight = "Right";

        public UIComponentHorizontalGridContainer()
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

        protected virtual IEnumerable<UIComponentConfiguration> GetComponents(string alignment)
        {
            return this.Configuration.Children.Where(component =>
            {
                var value = default(string);
                if (component.MetaData.TryGetValue(Alignment, out value))
                {
                    return string.Equals(value, alignment, StringComparison.OrdinalIgnoreCase);
                }
                //Align left by default.
                return string.Equals(alignment, AlignLeft, StringComparison.OrdinalIgnoreCase);
            });
        }

        protected virtual void UpdateChildren()
        {
            this.Grid.Children.Clear(UIDisposerFlags.Default);
            this.Grid.ColumnDefinitions.Clear();
            if (this.Configuration.Children.Count > 0)
            {
                this.AddLeft(this.GetComponents(AlignLeft));
                this.AddStretch(this.GetComponents(AlignStretch));
                this.AddRight(this.GetComponents(AlignRight));
            }
            else
            {
                var component = new UIComponentConfiguration();
                this.AddLeft(new[] { component });
                this.Configuration.Children.Add(component);
            }
        }

        protected virtual void AddLeft(IEnumerable<UIComponentConfiguration> components)
        {
            this.AddContainer(components, HorizontalAlignment.Left, new GridLength(0, GridUnitType.Auto));
        }

        protected virtual void AddStretch(IEnumerable<UIComponentConfiguration> components)
        {
            this.AddContainer(components, HorizontalAlignment.Stretch, new GridLength(1, GridUnitType.Star));
        }

        protected virtual void AddRight(IEnumerable<UIComponentConfiguration> components)
        {
            this.AddContainer(components, HorizontalAlignment.Right, new GridLength(0, GridUnitType.Auto));
        }

        protected virtual void AddContainer(IEnumerable<UIComponentConfiguration> components, HorizontalAlignment alignment, GridLength width)
        {
            if (!components.Any())
            {
                //Create an empty column so other things align correctly.
                this.Grid.ColumnDefinitions.Add(new ColumnDefinition()
                {
                    Width = width
                });
                return;
            }
            foreach (var component in components)
            {
                this.AddContainer(alignment, width, component);
            }
        }

        protected virtual void AddContainer(HorizontalAlignment alignment, GridLength width, UIComponentConfiguration component)
        {
            var margin = default(Thickness);
            if (this.Grid.ColumnDefinitions.Count > 0)
            {
                margin = new Thickness(2, 0, 0, 0);
            }
            this.Grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = width
            });
            var container = new UIComponentContainer()
            {
                Configuration = component,
                Margin = margin,
                HorizontalAlignment = alignment,
            };
            var eventHandler = new RoutedPropertyChangedEventHandler<UIComponentConfiguration>((sender, e) =>
            {
                this.UpdateComponent(component, container.Configuration);
            });
            container.ConfigurationChanged += eventHandler;
            this.EventHandlers.Add(container, eventHandler);
            Grid.SetColumn(container, this.Grid.ColumnDefinitions.Count - 1);
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
                var alignment = default(string);
                var container = UIComponentDesignerOverlay.Container;
                if (container != null)
                {
                    if (!container.Configuration.MetaData.TryGetValue(Alignment, out alignment))
                    {
                        //Align left by default.
                        alignment = AlignLeft;
                    }
                }
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    ADD,
                    Strings.UIComponentGridContainer_Add
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    REMOVE,
                    Strings.UIComponentGridContainer_Remove
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    MOVE_LEFT,
                    Strings.UIComponentGridContainer_MoveLeft,
                    attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    MOVE_RIGHT,
                    Strings.UIComponentGridContainer_MoveRight
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    ALIGN_LEFT,
                    Strings.UIComponentGridContainer_AlignLeft,
                    attributes: (byte)((string.Equals(alignment, AlignLeft, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    ALIGN_STRETCH,
                    Strings.UIComponentGridContainer_AlignStretch,
                    attributes: string.Equals(alignment, AlignStretch, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    ALIGN_RIGHT,
                    Strings.UIComponentGridContainer_AlignRight,
                    attributes: string.Equals(alignment, AlignRight, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            if (component.Source is UIComponentContainer container)
            {
                switch (component.Id)
                {
                    case ADD:
                        return this.Add(container);
                    case REMOVE:
                        return this.Remove(container);
                    case MOVE_LEFT:
                        return this.MoveLeft(container);
                    case MOVE_RIGHT:
                        return this.MoveRight(container);
                    case ALIGN_LEFT:
                        return this.SetAlignment(container, AlignLeft);
                    case ALIGN_STRETCH:
                        return this.SetAlignment(container, AlignStretch);
                    case ALIGN_RIGHT:
                        return this.SetAlignment(container, AlignRight);
                }
            }
            else
            {
                switch (component.Id)
                {
                    case ADD:
                        return this.Add();
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

        public Task Add(UIComponentContainer container)
        {
            return Windows.Invoke(() =>
            {
                var component = new UIComponentConfiguration();
                var index = this.Configuration.Children.IndexOf(container.Configuration);
                var alignment = default(string);
                if (container.Configuration.MetaData.TryGetValue(Alignment, out alignment))
                {
                    component.MetaData.TryAdd(Alignment, alignment);
                }
                this.Configuration.Children.Insert(index + 1, component);
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

        public Task MoveLeft(UIComponentContainer container)
        {
            return this.Move(container, index => index - 1);
        }

        public Task MoveRight(UIComponentContainer container)
        {
            return this.Move(container, index => index + 1);
        }

        protected virtual Task Move(UIComponentContainer container, Func<int, int> step)
        {
            return Windows.Invoke(() =>
            {
                var containerAlignment = default(string);
                if (!container.Configuration.MetaData.TryGetValue(Alignment, out containerAlignment))
                {
                    //Align left by default.
                    containerAlignment = AlignLeft;
                }
                for (var a = 0; a < this.Configuration.Children.Count; a++)
                {
                    if (!object.ReferenceEquals(this.Configuration.Children[a], container.Configuration))
                    {
                        continue;
                    }
                    for (var b = step(a); b >= 0 && b < this.Configuration.Children.Count; b = step(b))
                    {
                        var childAlignment = default(string);
                        if (!this.Configuration.Children[b].MetaData.TryGetValue(Alignment, out childAlignment))
                        {
                            //Align left by default.
                            childAlignment = AlignLeft;
                        }
                        if (!string.Equals(containerAlignment, childAlignment, StringComparison.OrdinalIgnoreCase))
                        {
                            //Alignment differs.
                            continue;
                        }
                        this.Configuration.Children[a] = this.Configuration.Children[b];
                        this.Configuration.Children[b] = container.Configuration;
                        this.UpdateChildren();
                        return;
                    }
                    //TODO: Move is invalid.
                    return;
                }
            });
        }

        public Task SetAlignment(UIComponentContainer container, string alignment)
        {
            return Windows.Invoke(() =>
            {
                if (container.Configuration == null)
                {
                    container.Configuration = new UIComponentConfiguration();
                }
                container.Configuration.MetaData.AddOrUpdate(Alignment, alignment);
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
