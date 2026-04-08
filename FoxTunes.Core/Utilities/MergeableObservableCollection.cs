using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class MergeableObservableCollection<T> : ObservableCollection<T> where T : IPersistableComponent
    {
        public MergeableObservableCollection()
        {

        }

        public MergeableObservableCollection(IEnumerable<T> sequence) : base(sequence)
        {

        }

        public override Action Reset(IEnumerable<T> sequence)
        {
            lock (this.SyncRoot)
            {
                this.IsSuspended = true;
                try
                {
                    if (this.Count == 0)
                    {
                        this.AddRange(sequence);
                    }
                    else
                    {
                        var position = 0;
                        var map = this.ToDictionary(element => element.Id);
                        foreach (var element in sequence)
                        {
                            var existing = default(T);
                            if (position < this.Count && this[position].Id == element.Id)
                            {
                                this.Update(element, this[position]);
                            }
                            else if (map.TryGetValue(element.Id, out existing))
                            {
                                this.Update(element, existing);
                                this.Remove(existing);
                                this.Insert(position, existing);
                            }
                            else
                            {
                                this.Insert(position, element);
                            }
                            position++;
                        }
                        while (this.Count > sequence.Count())
                        {
                            this.RemoveAt(this.Count - 1);
                        }
                    }
                }
                finally
                {
                    this.IsSuspended = false;
                }
            }
            return this.Emit();
        }

        protected virtual void Update(T source, T destination)
        {
            //Nothing to do.
        }
    }
}
