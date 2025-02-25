using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public static partial class ListViewExtensions
    {
        public const int MAX_SELECTED_ITEMS = 750;

        private static readonly ConditionalWeakTable<ListView, SelectedItemsBehaviour> SelectedItemsBehaviours = new ConditionalWeakTable<ListView, SelectedItemsBehaviour>();

        public static bool IsSuspended { get; private set; }

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnSelectedItemsPropertyChanged))
        );

        public static IList GetSelectedItems(ListView source)
        {
            return (IList)source.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(ListView source, IList value)
        {
            source.SetValue(SelectedItemsProperty, value);
        }

        private static void OnSelectedItemsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsSuspended)
            {
                return;
            }
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            var behaviour = default(SelectedItemsBehaviour);
            if (!SelectedItemsBehaviours.TryGetValue(listView, out behaviour))
            {
                SelectedItemsBehaviours.Add(listView, new SelectedItemsBehaviour(listView));
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
                if (Enumerable.SequenceEqual(listView.SelectedItems.Cast<object>(), items.Cast<object>()))
                {
                    return;
                }
                listView.SelectedItems.Clear();
                foreach (var item in items)
                {
                    listView.SelectedItems.Add(item);
                }
            }
            finally
            {
                IsSuspended = false;
            }
        }

        private class SelectedItemsBehaviour : UIBehaviour<ListView>
        {
            public SelectedItemsBehaviour(ListView listView) : base(listView)
            {
                this.ListView = listView;
                this.ListView.SelectionChanged += this.OnSelectionChanged;
            }

            public ListView ListView { get; private set; }

            protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                SetSelectedItems(this.ListView, this.ListView.SelectedItems);
            }

            protected override void OnDisposing()
            {
                if (this.ListView != null)
                {
                    this.ListView.SelectionChanged -= this.OnSelectionChanged;
                }
                base.OnDisposing();
            }
        }
    }
}
