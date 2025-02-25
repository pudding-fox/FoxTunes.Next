using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public static partial class GridViewColumnExtensions
    {
        private static readonly ConditionalWeakTable<GridViewColumn, PositionBehaviour> PositionBehaviours = new ConditionalWeakTable<GridViewColumn, PositionBehaviour>();

        public static readonly DependencyProperty PositionProperty = DependencyProperty.RegisterAttached(
            "Position",
            typeof(int?),
            typeof(GridViewColumnExtensions),
            new UIPropertyMetadata(null, new PropertyChangedCallback(OnPositionPropertyChanged))
        );

        public static int? GetPosition(GridViewColumn source)
        {
            return (int?)source.GetValue(PositionProperty);
        }

        public static void SetPosition(GridViewColumn source, int? value)
        {
            source.SetValue(PositionProperty, value);
        }

        private static void OnPositionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var gridViewColumn = sender as GridViewColumn;
            if (gridViewColumn == null)
            {
                return;
            }
            var behaviour = default(PositionBehaviour);
            if (!PositionBehaviours.TryGetValue(gridViewColumn, out behaviour))
            {
                PositionBehaviours.Add(gridViewColumn, new PositionBehaviour(gridViewColumn));
            }
        }

        public class PositionBehaviour : UIBehaviour<GridViewColumn>
        {
            private static volatile bool IsUpdating;

            public PositionBehaviour(GridViewColumn gridViewColumn) : base(gridViewColumn)
            {
                this.GridViewColumn = gridViewColumn;
                InheritanceContextHelper.AddEventHandler(this.GridViewColumn, this.OnInheritanceContextChanged);
            }

            public GridViewColumn GridViewColumn { get; private set; }

            public GridView GridView { get; private set; }

            protected virtual void OnInheritanceContextChanged(object sender, EventArgs e)
            {
                if (this.GridView != null)
                {
                    this.GridView.Columns.CollectionChanged -= this.OnColumnsChanged;
                }
                this.GridView = InheritanceContextHelper.Get(this.GridViewColumn) as GridView;
                if (this.GridView != null)
                {
                    this.GridView.Columns.CollectionChanged += this.OnColumnsChanged;
                }
            }

            protected virtual void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (IsUpdating)
                {
                    return;
                }
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Reset:
                        this.GridView.Dispatcher.BeginInvoke(new Action(this.UpdateTarget));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        this.UpdateSource();
                        break;
                }
            }

            protected virtual void UpdateTarget()
            {
                if (this.GridView == null || !GetPosition(this.GridViewColumn).HasValue)
                {
                    return;
                }
                var position = this.GridView.Columns.IndexOf(this.GridViewColumn);
                if (position < 0 || position == GetPosition(this.GridViewColumn))
                {
                    return;
                }
                IsUpdating = true;
                try
                {
                    this.GridView.Columns.Move(position, Math.Min(this.GridView.Columns.Count - 1, GetPosition(this.GridViewColumn).Value));
                }
                finally
                {
                    IsUpdating = false;
                }
            }

            protected virtual void UpdateSource()
            {
                var position = this.GridView.Columns.IndexOf(this.GridViewColumn);
                if (position == GetPosition(this.GridViewColumn))
                {
                    return;
                }
                SetPosition(this.GridViewColumn, position);
            }

            protected override void OnDisposing()
            {
                if (this.GridView != null)
                {
                    this.GridView.Columns.CollectionChanged -= this.OnColumnsChanged;
                }
                if (this.GridViewColumn != null)
                {
                    InheritanceContextHelper.RemoveEventHandler(this.GridViewColumn, this.OnInheritanceContextChanged);
                }
                base.OnDisposing();
            }
        }
    }
}
