using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public static partial class ContextMenuExtensions
    {
        private static readonly ConditionalWeakTable<ContextMenu, ItemContainerStyleBehaviour> ItemContainerStyleBehaviours = new ConditionalWeakTable<ContextMenu, ItemContainerStyleBehaviour>();

        public static readonly DependencyProperty ItemContainerStyleProperty = DependencyProperty.RegisterAttached(
            "ItemContainerStyle",
            typeof(Style),
            typeof(ContextMenuExtensions),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnItemContainerStylePropertyChanged))
        );

        public static Style GetItemContainerStyle(ContextMenu source)
        {
            return (Style)source.GetValue(ItemContainerStyleProperty);
        }

        public static void SetItemContainerStyle(ContextMenu source, Style value)
        {
            source.SetValue(ItemContainerStyleProperty, value);
        }

        private static void OnItemContainerStylePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            if (contextMenu == null)
            {
                return;
            }
            if (GetItemContainerStyle(contextMenu) != null)
            {
                var behaviour = default(ItemContainerStyleBehaviour);
                if (!ItemContainerStyleBehaviours.TryGetValue(contextMenu, out behaviour))
                {
                    ItemContainerStyleBehaviours.Add(contextMenu, new ItemContainerStyleBehaviour(contextMenu));
                }
            }
            else
            {
                ItemContainerStyleBehaviours.Remove(contextMenu);
            }
        }

        private class ItemContainerStyleBehaviour : DynamicStyleBehaviour<ContextMenu>
        {
            public ItemContainerStyleBehaviour(ContextMenu contextMenu) : base(contextMenu)
            {
                this.ContextMenu = contextMenu;
                this.Apply();
            }

            public ContextMenu ContextMenu { get; private set; }

            protected override void Apply()
            {
                this.ContextMenu.ItemContainerStyle = this.CreateStyle(
                    GetItemContainerStyle(this.ContextMenu),
                    (Style)this.ContextMenu.TryFindResource(typeof(MenuItem))
                );
            }
        }
    }
}
