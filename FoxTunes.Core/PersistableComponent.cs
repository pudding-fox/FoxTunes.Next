using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public abstract class PersistableComponent : BaseComponent, IPersistableComponent
    {
        public int Id { get; set; }

        public override string ToString()
        {
            return Convert.ToString(this.Id);
        }

        public virtual bool Equals(IPersistableComponent other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (this.Id == 0)
            {
                //Un-persisted data is never equal.
                return false;
            }
            return this.Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as IPersistableComponent);
        }

        public override int GetHashCode()
        {
            //This is awkward. I'd like to return this.Id.GetHashCode() but WPF gets mad when 
            //new data is persisted (thus changing it's hash code).
            //Everything seems to "work" with a fixed value.
            return 0;
        }

        public static bool operator ==(PersistableComponent a, PersistableComponent b)
        {
            if ((object)a == null && (object)b == null)
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            if (object.ReferenceEquals((object)a, (object)b))
            {
                return true;
            }
            return a.Equals(b);
        }

        public static bool operator !=(PersistableComponent a, PersistableComponent b)
        {
            return !(a == b);
        }
    }
}
