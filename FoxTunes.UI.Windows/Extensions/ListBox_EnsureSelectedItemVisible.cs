using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FoxTunes
{
    public static partial class ListBoxExtensions
    {
        private static readonly ConditionalWeakTable<ListBox, EnsureSelectedItemVisibleBehaviour> EnsureSelectedItemVisibleBehaviours = new ConditionalWeakTable<ListBox, EnsureSelectedItemVisibleBehaviour>();

        public static readonly DependencyProperty EnsureSelectedItemVisibleProperty = DependencyProperty.RegisterAttached(
            "EnsureSelectedItemVisible",
            typeof(bool),
            typeof(ListBoxExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnEnsureSelectedItemVisiblePropertyChanged))
        );

        public static bool GetEnsureSelectedItemVisible(ListBox source)
        {
            return (bool)source.GetValue(EnsureSelectedItemVisibleProperty);
        }

        public static void SetEnsureSelectedItemVisible(ListBox source, bool value)
        {
            source.SetValue(EnsureSelectedItemVisibleProperty, value);
        }

        private static void OnEnsureSelectedItemVisiblePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null)
            {
                return;
            }
            if (GetEnsureSelectedItemVisible(listBox))
            {
                var behaviour = default(EnsureSelectedItemVisibleBehaviour);
                if (!EnsureSelectedItemVisibleBehaviours.TryGetValue(listBox, out behaviour))
                {
                    EnsureSelectedItemVisibleBehaviours.Add(listBox, new EnsureSelectedItemVisibleBehaviour(listBox));
                }
            }
            else
            {
                var behaviour = default(EnsureSelectedItemVisibleBehaviour);
                if (EnsureSelectedItemVisibleBehaviours.TryGetValue(listBox, out behaviour))
                {
                    EnsureSelectedItemVisibleBehaviours.Remove(listBox);
                    behaviour.Dispose();
                }
            }
        }

        private class EnsureSelectedItemVisibleBehaviour : UIBehaviour<ListBox>
        {
            public EnsureSelectedItemVisibleBehaviour(ListBox listBox) : base(listBox)
            {
                this.ListBox = listBox;
                this.ListBox.SelectionChanged += this.OnSelectionChanged;
            }

            public ListBox ListBox { get; private set; }

            protected virtual void EnsureVisible(object value)
            {
                if (value == null)
                {
                    return;
                }
                var action = new Action(() =>
                {
                    this.ListBox.ScrollIntoView(value);
                    this.ListBox.UpdateLayout();
                    var container = this.ListBox.ItemContainerGenerator.ContainerFromItem(value) as ListViewItem;
                    if (container != null)
                    {
                        container.BringIntoView();
                    }
                });
                this.ListBox.Dispatcher.BeginInvoke(
                    action,
                    DispatcherPriority.Loaded
                );
            }

            protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                this.EnsureVisible(this.ListBox.SelectedItem);
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
