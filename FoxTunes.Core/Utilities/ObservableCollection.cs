using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FoxTunes
{
    public class ObservableCollection<T> : global::System.Collections.ObjectModel.ObservableCollection<T>
    {
        private const string COUNT = "Count";

        private const string INDEXER = "Item[]";

        public readonly object SyncRoot = new object();

        public ObservableCollection()
        {

        }

        public ObservableCollection(IEnumerable<T> sequence) : base(sequence)
        {

        }

        public bool IsSuspended { get; private set; }

        public Action Reset(IEnumerable<T> sequence)
        {
            lock (this.SyncRoot)
            {
                this.IsSuspended = true;
                try
                {
                    this.Clear();
                    this.AddRange(sequence);
                }
                finally
                {
                    this.IsSuspended = false;
                }
            }
            return this.Emit();
        }

        protected virtual Action Emit()
        {
            return () =>
            {
                this.OnPropertyChanged(new PropertyChangedEventArgs(COUNT));
                this.OnPropertyChanged(new PropertyChangedEventArgs(INDEXER));
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            };
        }


        public void ResetSync(IEnumerable<T> sequence)
        {
            this.Reset(sequence)();
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.IsSuspended)
            {
                return;
            }
            base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.IsSuspended)
            {
                return;
            }
            base.OnPropertyChanged(e);
        }
    }
}
