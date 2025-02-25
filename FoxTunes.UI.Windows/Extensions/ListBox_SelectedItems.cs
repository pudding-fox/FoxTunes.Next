using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public static partial class ListBoxExtensions
    {
        public const int MAX_SELECTED_ITEMS = 750;

        private static readonly ConditionalWeakTable<ListBox, SelectedItemsBehaviour> SelectedItemsBehaviours = new ConditionalWeakTable<ListBox, SelectedItemsBehaviour>();

        public static bool IsSuspended { get; private set; }

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(ListBoxExtensions),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnSelectedItemsPropertyChanged))
        );

        public static IList GetSelectedItems(ListBox source)
        {
            return (IList)source.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(ListBox source, IList value)
        {
            source.SetValue(SelectedItemsProperty, value);
        }

        private static void OnSelectedItemsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsSuspended)
            {
                return;
            }
            var listBox = sender as ListBox;
            if (listBox == null)
            {
                return;
            }
            var behaviour = default(SelectedItemsBehaviour);
            if (!SelectedItemsBehaviours.TryGetValue(listBox, out behaviour))
            {
                SelectedItemsBehaviours.Add(listBox, new SelectedItemsBehaviour(listBox));
            }
            IsSuspended = true;
            try
            {
                //We only need this if we need two way binding (it's sketchy and causes weird recursive 
                //calls to this handler anyway so let's just not).
                var items = (e.NewValue as IList);
                if (items == null)
                {
                    return;
                }
                if (Enumerable.SequenceEqual(listBox.SelectedItems.Cast<object>(), items.Cast<object>()))
                {
                    return;
                }
                listBox.SelectedItems.Clear();
                foreach (var item in items)
                {
                    listBox.SelectedItems.Add(item);
                }
            }
            finally
            {
                IsSuspended = false;
            }
        }

        private class SelectedItemsBehaviour : UIBehaviour<ListBox>
        {
            public SelectedItemsBehaviour(ListBox listBox) : base(listBox)
            {
                this.ListBox = listBox;
                this.ListBox.SelectionChanged += this.OnSelectionChanged;
            }

            public ListBox ListBox { get; private set; }

            protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                SetSelectedItems(this.ListBox, this.ListBox.SelectedItems);
            }

            protected override void OnDisposing()
            {
                if (this.ListBox != null)
                {
                    this.ListBox.SelectionChanged -= this.OnSelectionChanged;
                }
                base.OnDisposing();
            }
        }
    }
}
