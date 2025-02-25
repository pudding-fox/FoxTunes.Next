using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public static partial class ListBoxExtensions
    {
        private static readonly ConditionalWeakTable<ListBox, ItemContainerStyleBehaviour> ItemContainerStyleBehaviours = new ConditionalWeakTable<ListBox, ItemContainerStyleBehaviour>();

        public static readonly DependencyProperty ItemContainerStyleProperty = DependencyProperty.RegisterAttached(
            "ItemContainerStyle",
            typeof(Style),
            typeof(ListBoxExtensions),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnItemContainerStylePropertyChanged))
        );

        public static Style GetItemContainerStyle(ListBox source)
        {
            return (Style)source.GetValue(ItemContainerStyleProperty);
        }

        public static void SetItemContainerStyle(ListBox source, Style value)
        {
            source.SetValue(ItemContainerStyleProperty, value);
        }

        private static void OnItemContainerStylePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null)
            {
                return;
            }
            if (GetItemContainerStyle(listBox) != null)
            {
                var behaviour = default(ItemContainerStyleBehaviour);
                if (!ItemContainerStyleBehaviours.TryGetValue(listBox, out behaviour))
                {
                    ItemContainerStyleBehaviours.Add(listBox, new ItemContainerStyleBehaviour(listBox));
                }
            }
            else
            {
                ItemContainerStyleBehaviours.Remove(listBox);
            }
        }

        private class ItemContainerStyleBehaviour : DynamicStyleBehaviour<ListBox>
        {
            public ItemContainerStyleBehaviour(ListBox listBox) : base(listBox)
            {
                this.ListBox = listBox;
                this.Apply();
            }

            public ListBox ListBox { get; private set; }

            protected override void Apply()
            {
                this.ListBox.ItemContainerStyle = this.CreateStyle(
                    GetItemContainerStyle(this.ListBox),
                    (Style)this.ListBox.TryFindResource(typeof(ListBoxItem))
                );
            }
        }
    }
}
