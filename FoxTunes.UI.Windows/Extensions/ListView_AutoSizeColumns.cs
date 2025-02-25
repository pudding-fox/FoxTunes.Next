using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class ListViewExtensions
    {
        private static readonly ConditionalWeakTable<ListView, AutoSizeColumnsBehaviour> AutoSizeColumnsBehaviours = new ConditionalWeakTable<ListView, AutoSizeColumnsBehaviour>();

        public static readonly DependencyProperty AutoSizeColumnsProperty = DependencyProperty.RegisterAttached(
            "AutoSizeColumns",
            typeof(bool),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnAutoSizeColumnsPropertyChanged))
        );

        public static bool GetAutoSizeColumns(ListView source)
        {
            return (bool)source.GetValue(AutoSizeColumnsProperty);
        }

        public static void SetAutoSizeColumns(ListView source, bool value)
        {
            source.SetValue(AutoSizeColumnsProperty, value);
        }

        private static void OnAutoSizeColumnsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            if (GetAutoSizeColumns(listView))
            {
                var behaviour = default(AutoSizeColumnsBehaviour);
                if (!AutoSizeColumnsBehaviours.TryGetValue(listView, out behaviour))
                {
                    AutoSizeColumnsBehaviours.Add(listView, new AutoSizeColumnsBehaviour(listView));
                }
            }
            else
            {
                AutoSizeColumnsBehaviours.Remove(listView);
            }
        }

        private class AutoSizeColumnsBehaviour : UIBehaviour<ListView>
        {
            public static readonly TimeSpan TIMEOUT = TimeSpan.FromMilliseconds(500);

            public AutoSizeColumnsBehaviour(ListView listView) : base(listView)
            {
                this.Columns = new Dictionary<GridViewColumn, IList<AutoSizeColumn>>();
                this.Debouncer = new Debouncer(TIMEOUT);
                this.ListView = listView;
                this.ListView.ItemContainerGenerator.StatusChanged += this.OnStatusChanged;
            }

            public ListView ListView { get; private set; }

            public IDictionary<GridViewColumn, IList<AutoSizeColumn>> Columns { get; private set; }

            public Debouncer Debouncer { get; private set; }

            protected virtual void OnStatusChanged(object sender, EventArgs e)
            {
                if (this.ListView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                {
                    return;
                }
                this.UpdateColumns();
            }

            protected virtual void UpdateColumns()
            {
                var gridView = this.ListView.View as GridView;
                if (gridView == null)
                {
                    return;
                }
                this.Reset();
                for (int a = 0, b = this.ListView.Items.Count; a < b; a++)
                {
                    for (var c = 0; c < gridView.Columns.Count; c++)
                    {
                        var gridViewColumn = gridView.Columns[c];
                        if (!double.IsNaN(gridViewColumn.Width))
                        {
                            continue;
                        }
                        this.UpdateColumn(gridView, gridViewColumn, a);
                    }
                }
            }

            protected virtual void UpdateColumn(GridView gridView, GridViewColumn gridViewColumn, int row)
            {
                var item = this.ListView.ItemContainerGenerator.ContainerFromIndex(row);
                if (item == null)
                {
                    return;
                }
                var rowPresenter = item.FindChild<GridViewRowPresenter>();
                if (rowPresenter == null)
                {
                    return;
                }
                //TODO: Only use this logic for TextBlock as other types of content are likely to have a static width.
                var content = VisualTreeHelper.GetChild(rowPresenter, gridView.Columns.IndexOf(gridViewColumn)) as TextBlock;
                if (content == null)
                {
                    return;
                }
                this.UpdateColumn(gridViewColumn, content);
            }

            protected virtual void UpdateColumn(GridViewColumn gridViewColumn, FrameworkElement content)
            {
                var column = new AutoSizeColumn(this, content);
                this.Columns.GetOrAdd(gridViewColumn, () => new List<AutoSizeColumn>()).Add(column);
            }

            protected virtual void CheckColumns()
            {
                this.Debouncer.Exec(() =>
                {
                    var task = Windows.Invoke(() =>
                    {
                        foreach (var pair in this.Columns)
                        {
                            this.CheckColumns(pair.Key, pair.Value);
                        }
                    });
                });
            }

            protected virtual void CheckColumns(GridViewColumn gridViewColumn, IEnumerable<AutoSizeColumn> columns)
            {
                var width = default(double);
                foreach (var column in columns)
                {
                    width = Math.Max(column.Content.ActualWidth, width);
                }
                if (width > gridViewColumn.ActualWidth)
                {
                    PlaylistGridViewColumnFactory.BeginAutoSize(gridViewColumn);
                    gridViewColumn.Width = gridViewColumn.ActualWidth;
                    gridViewColumn.Width = double.NaN;
                    PlaylistGridViewColumnFactory.EndAutoSize(gridViewColumn);
                }
            }

            protected virtual void Reset()
            {
                foreach (var pair in this.Columns)
                {
                    foreach (var column in pair.Value)
                    {
                        column.Dispose();
                    }
                }
                this.Columns.Clear();
            }

            protected override void OnDisposing()
            {
                if (this.Debouncer != null)
                {
                    this.Debouncer.Dispose();
                }
                if (this.ListView != null && this.ListView.ItemContainerGenerator != null)
                {
                    this.ListView.ItemContainerGenerator.StatusChanged -= this.OnStatusChanged;
                }
                this.Reset();
                base.OnDisposing();
            }

            public class AutoSizeColumn : IDisposable
            {
                public AutoSizeColumn(AutoSizeColumnsBehaviour behaviour, FrameworkElement content)
                {
                    this.Behaviour = behaviour;
                    this.Content = content;
                    this.Content.SizeChanged += this.OnSizeChanged;
                }

                public AutoSizeColumnsBehaviour Behaviour { get; private set; }

                public FrameworkElement Content { get; private set; }

                protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e)
                {
                    this.Behaviour.CheckColumns();
                }

                public bool IsDisposed { get; private set; }

                public void Dispose()
                {
                    this.Dispose(true);
                    GC.SuppressFinalize(this);
                }

                protected virtual void Dispose(bool disposing)
                {
                    if (this.IsDisposed || !disposing)
                    {
                        return;
                    }
                    this.OnDisposing();
                    this.IsDisposed = true;
                }

                protected virtual void OnDisposing()
                {
                    if (this.Content != null)
                    {
                        this.Content.SizeChanged -= this.OnSizeChanged;
                    }
                }

                ~AutoSizeColumn()
                {
                    Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
                    try
                    {
                        this.Dispose(true);
                    }
                    catch
                    {
                        //Nothing can be done, never throw on GC thread.
                    }
                }
            }
        }
    }
}
