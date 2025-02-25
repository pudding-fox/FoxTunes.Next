using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes
{
    public class MultiSelectFilterableListBox : ContentControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(IEnumerable),
            typeof(MultiSelectFilterableListBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnItemsSourcePropertyChanged))
        );

        public static IEnumerable GetItemsSource(MultiSelectFilterableListBox source)
        {
            return (IEnumerable)source.GetValue(ItemsSourceProperty);
        }

        public static void SetItemsSource(MultiSelectFilterableListBox source, IEnumerable value)
        {
            source.SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var multiSelectFilterableListBox = sender as MultiSelectFilterableListBox;
            if (multiSelectFilterableListBox == null)
            {
                return;
            }
            multiSelectFilterableListBox.OnItemsSourceChanged();
        }

        public static readonly DependencyProperty FilteredItemsSourceProperty = DependencyProperty.Register(
            "FilteredItemsSource",
            typeof(IEnumerable),
            typeof(MultiSelectFilterableListBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnFilteredItemsSourcePropertyChanged))
        );

        public static IEnumerable GetFilteredItemsSource(MultiSelectFilterableListBox source)
        {
            return (IEnumerable)source.GetValue(FilteredItemsSourceProperty);
        }

        public static void SetFilteredItemsSource(MultiSelectFilterableListBox source, IEnumerable value)
        {
            source.SetValue(FilteredItemsSourceProperty, value);
        }

        private static void OnFilteredItemsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var multiSelectFilterableListBox = sender as MultiSelectFilterableListBox;
            if (multiSelectFilterableListBox == null)
            {
                return;
            }
            multiSelectFilterableListBox.OnFilteredItemsSourceChanged();
        }

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            "SelectedItems",
            typeof(IEnumerable),
            typeof(MultiSelectFilterableListBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnSelectedItemsPropertyChanged))
        );

        public static IEnumerable GetSelectedItems(MultiSelectFilterableListBox source)
        {
            return (IEnumerable)source.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(MultiSelectFilterableListBox source, IEnumerable value)
        {
            source.SetValue(SelectedItemsProperty, value);
        }

        private static void OnSelectedItemsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var multiSelectFilterableListBox = sender as MultiSelectFilterableListBox;
            if (multiSelectFilterableListBox == null)
            {
                return;
            }
            multiSelectFilterableListBox.OnSelectedItemsChanged();
        }

        public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
            "Filter",
            typeof(string),
            typeof(MultiSelectFilterableListBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnFilterPropertyChanged))
        );

        public static string GetFilter(MultiSelectFilterableListBox source)
        {
            return (string)source.GetValue(FilterProperty);
        }

        public static void SetFilter(MultiSelectFilterableListBox source, string value)
        {
            source.SetValue(FilterProperty, value);
        }

        private static void OnFilterPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var multiSelectFilterableListBox = sender as MultiSelectFilterableListBox;
            if (multiSelectFilterableListBox == null)
            {
                return;
            }
            multiSelectFilterableListBox.OnFilterChanged();
        }

        public MultiSelectFilterableListBox()
        {
            this.ListBox = new ListBox()
            {
                SelectionMode = SelectionMode.Multiple
            };
            this.ListBox.SelectionChanged += this.OnSelectionChanged;
            this.ListBox.SetBinding(ListBox.ItemsSourceProperty, new Binding("FilteredItemsSource")
            {
                Source = this
            });
        }

        public bool IsUpdating { get; private set; }

        public ListBox ListBox
        {
            get
            {
                return this.Content as ListBox;
            }
            set
            {
                this.Content = value;
            }
        }

        public IEnumerable ItemsSource
        {
            get
            {
                return GetItemsSource(this);
            }
            set
            {
                SetItemsSource(this, value);
            }
        }

        protected virtual void OnItemsSourceChanged()
        {
            this.UpdateFilteredItems();
            this.UpdateSelectionFromSource();
            if (this.ItemsSourceChanged == null)
            {
                return;
            }
            this.ItemsSourceChanged(this, EventArgs.Empty);
        }

        public event EventHandler ItemsSourceChanged;

        public IEnumerable FilteredItemsSource
        {
            get
            {
                return GetFilteredItemsSource(this);
            }
            set
            {
                SetFilteredItemsSource(this, value);
            }
        }

        protected virtual void OnFilteredItemsSourceChanged()
        {
            if (this.FilteredItemsSourceChanged == null)
            {
                return;
            }
            this.FilteredItemsSourceChanged(this, EventArgs.Empty);
        }

        public event EventHandler FilteredItemsSourceChanged;

        public IEnumerable SelectedItems
        {
            get
            {
                return GetSelectedItems(this);
            }
            set
            {
                SetSelectedItems(this, value);
            }
        }

        protected virtual void OnSelectedItemsChanged()
        {
            this.UpdateSelectionFromSource();
            if (this.SelectedItemsChanged == null)
            {
                return;
            }
            this.SelectedItemsChanged(this, EventArgs.Empty);
        }

        public event EventHandler SelectedItemsChanged;

        public string Filter
        {
            get
            {
                return GetFilter(this);
            }
            set
            {
                SetFilter(this, value);
            }
        }

        protected virtual void OnFilterChanged()
        {
            this.UpdateFilteredItems();
            this.UpdateSelectionFromSource();
            if (this.FilterChanged == null)
            {
                return;
            }
            this.FilterChanged(this, EventArgs.Empty);
        }

        public event EventHandler FilterChanged;

        protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.UpdateSelectionFromTarget();
        }

        protected virtual void UpdateFilteredItems()
        {
            this.IsUpdating = true;
            try
            {
                if (this.ItemsSource == null || string.IsNullOrEmpty(this.Filter))
                {
                    this.FilteredItemsSource = this.ItemsSource;
                }
                else
                {
                    this.FilteredItemsSource = this.GetVisibleItems(this.ItemsSource, this.Filter);
                }
            }
            finally
            {
                this.IsUpdating = false;
            }
        }

        protected virtual void UpdateSelectionFromSource()
        {
            if (this.IsUpdating)
            {
                return;
            }
            this.IsUpdating = true;
            try
            {
                this.ListBox.SelectedItems.Clear();
                if (this.SelectedItems == null)
                {
                    return;
                }
                foreach (var item in this.SelectedItems)
                {
                    if (this.IsItemVisible(item))
                    {
                        this.ListBox.SelectedItems.Add(item);
                    }
                }
            }
            finally
            {
                this.IsUpdating = false;
            }
        }

        protected virtual void UpdateSelectionFromTarget()
        {
            if (this.IsUpdating)
            {
                return;
            }
            this.IsUpdating = true;
            try
            {
                var selectedItems = new List<object>();
                if (this.SelectedItems != null)
                {
                    var enumerator = this.SelectedItems.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        selectedItems.Add(enumerator.Current);
                    }
                }
                if (selectedItems != null)
                {
                    foreach (var item in this.FilteredItemsSource)
                    {
                        if (!this.ListBox.SelectedItems.Contains(item))
                        {
                            selectedItems.Remove(item);
                        }
                    }
                }
                else
                {
                    selectedItems = new List<object>();
                }
                foreach (var item in this.ListBox.SelectedItems)
                {
                    if (!selectedItems.Contains(item))
                    {
                        selectedItems.Add(item);
                    }
                }
                this.SelectedItems = selectedItems;
            }
            finally
            {
                this.IsUpdating = false;
            }
        }

        protected virtual bool IsItemVisible(object item)
        {
            if (this.FilteredItemsSource == null)
            {
                return false;
            }
            var enumerator = this.FilteredItemsSource.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (object.Equals(enumerator.Current, item))
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual IEnumerable GetVisibleItems(IEnumerable enumerable, string filter)
        {
            var result = new List<object>();
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.ToString().Contains(filter, true))
                {
                    result.Add(enumerator.Current);
                }
            }
            return result;
        }
    }
}
