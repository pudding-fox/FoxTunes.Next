using System;
using System.Linq;

namespace FoxTunes.Mpeg4
{
    public class XtraTagPart : IEquatable<XtraTagPart>
    {
        public XtraTagPart(XtraTagType type, byte[] content)
        {
            this.Type = type;
            this.Content = content;
        }

        public XtraTagType Type { get; private set; }

        public byte[] Content { get; private set; }

        public virtual bool Equals(XtraTagPart other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (this.Type != other.Type)
            {
                return false;
            }
            if (!Enumerable.SequenceEqual(this.Content, other.Content))
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as XtraTagPart);
        }

        public override int GetHashCode()
        {
            var hashCode = default(int);
            unchecked
            {
                hashCode += this.Type.GetHashCode();
            }
            return hashCode;
        }

        public static bool operator ==(XtraTagPart a, XtraTagPart b)
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

        public static bool operator !=(XtraTagPart a, XtraTagPart b)
        {
            return !(a == b);
        }
    }
}
